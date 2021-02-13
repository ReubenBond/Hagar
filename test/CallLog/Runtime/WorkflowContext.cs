using CallLog.Scheduling;
using Hagar;
using Hagar.Invocation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CallLog
{
    [GenerateMethodSerializers(typeof(WorkflowProxyBase))]
    internal interface IWorkflowControl
    {
        ValueTask OnActivationMarker(ActivationMarker marker);

        ValueTask CancelRunningRequests();
    }

    internal class WorkflowContext : IWorkflowContext, IWorkflowControl
    {
        private readonly object _lock = new object();
        private readonly Dictionary<long, RequestState> _outboundRequests = new Dictionary<long, RequestState>();
        private readonly Guid _invocationId;
        private readonly Channel<object> _logStage;
        private readonly ILogger<WorkflowContext> _log;
        private readonly LogManager _logManager;
        private readonly LogEnumerator _logEnumerator;
        private readonly MessageRouter _router;
        private readonly ActivationTaskScheduler _taskScheduler;
        private readonly IdSpan _id;
        private readonly Queue<Message> _messageQueue = new Queue<Message>();
        private readonly TargetHolder _targetHolder;
        private long _sequenceNumber;
        private Message _currentMessage;
        private object _instance;
        private Task _runTask;

        public WorkflowContext(
            IdSpan id,
            ILogger<WorkflowContext> log,
            ILogger<ActivationTaskScheduler> taskSchedulerLogger,
            LogManager logManager,
            LogEnumerator logEnumerator,
            MessageRouter router)
        {
            _invocationId = Guid.NewGuid();
            _logStage = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            _id = id;
            _log = log;
            _logManager = logManager;
            _logEnumerator = logEnumerator;
            _router = router;
            _taskScheduler = new ActivationTaskScheduler(this, CancellationToken.None, taskSchedulerLogger);
            _targetHolder = new TargetHolder(this);
        }

        public IdSpan Id => _id;

        public object Instance
        {
            get => _instance;
            set
            {
                _instance = value;
                if (value is IHasWorkflowContext hasContext)
                {
                    hasContext.Context = this;
                }
            }
        }

        public int MaxSupportedVersion { get; set; } = 2;

        public int CurrentVersion { get; set; }

        public async ValueTask ActivateAsync()
        {
            _ = Task.Factory.StartNew(state => ((WorkflowContext)state).RecoverAsync(), this, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
            await Task.CompletedTask;
        }

        public async ValueTask DeactivateAsync()
        {
            var previousContext = RuntimeContext.Current;
            try
            {
                RuntimeContext.Current = this;
                _logStage.Writer.TryComplete();
                if (_runTask is Task task)
                {
                    await task.ConfigureAwait(false);
                }
            }
            finally
            {
                RuntimeContext.Current = previousContext;
            }
        }

        public async Task RecoverAsync()
        {
            try
            {
                RuntimeContext.Current = this;

                // Write a marker to signify the creation of a new activation.
                // This is used for:
                //  * Workflow versioning, so that workflow code can evolve over time while remaining deterministic.
                //  * Recovery, so that the workflow knows when it has recovered and can begin processing incoming requests.
                //  * Diagnostics, so that the log can be inspected to see where each run began.
                await _logManager.EnqueueLogEntryAndWaitForCommitAsync(
                    _id,
                    new Message
                    {
                        SenderId = _id,
                        SequenceNumber = -1, // weird
                        Body = new ActivationMarker
                        {
                            InvocationId = _invocationId,
                            Time = DateTime.UtcNow,
                            Version = MaxSupportedVersion,
                        }
                    });

                // Enumerate all log messages into buffer.
                var recoveredEntries = new List<object>();
                var entries = _logEnumerator.GetCommittedLogEntries(_id);
                long lastResponseSeq = -1;
                await foreach (var entry in entries.Reader.ReadAllAsync())
                {
                    recoveredEntries.Add(entry);

                    if (entry is Message message)
                    {
                        var body = message.Body;
                        if (body is Response response)
                        {
                            // In order to prevent re-sending requests for which responses have already been received,
                            // record the response now. Additionally, record the order in which responses must be completed.
                            var sequenceNumber = message.SequenceNumber;
                            OnCommittedResponse(sequenceNumber, response, lastResponseSeq);
                            lastResponseSeq = sequenceNumber;
                        }
                        else if (body is ActivationMarker marker && marker.InvocationId == _invocationId)
                        {
                            // We have reached the end of the recovery portion of the log.
                            break;
                        }
                    }
                }

                foreach (var entry in recoveredEntries)
                {
                    OnCommittedMessage(entry);
                }

                // Start pumping newly added messages.
                // TODO: Ideally, we would be pumping them already (for performance), but just not sending them to the committed message handlers until now.
                _runTask = RunLogWriter();
            }
            catch (Exception exception)
            {
                _log.LogError(exception, "Error during recovery");
                throw;
            }
        }

        public ValueTask OnActivationMarker(ActivationMarker marker)
        {
            CurrentVersion = marker.Version;
            if (marker.InvocationId == _invocationId)
            {
                _log.LogInformation("{Id} recovered with InvocationId {InvocationId}", _id.ToString(), _invocationId);
            }
            else
            {
                _log.LogInformation("{Id} with InvocationId {InvocationId} encountered previous invocation with id {PreviousInvocationId}", _id.ToString(), _invocationId, marker.InvocationId);
            }

            return default;
        }

        public void OnCommittedMessage(object message)
        {
            switch (message)
            {
                case Message msg when msg.Body is Response:
                    OnCommittedResponse(msg);
                    break;
                case Message msg when msg.Body is IInvokable:
                    OnCommittedRequest(msg);
                    break;
                default:
                    _log.LogWarning("{Id} received unknown message {Message}", _id.ToString(), message);
                    break;
            }
        }

        public void OnCommittedResponse(Message message, long afterRequestNumber = -1) => OnCommittedResponse(message.SequenceNumber, (Response)message.Body, afterRequestNumber);

        public void OnCommittedResponse(long sequenceNumber, Response response, long afterResponseNumber = -1)
        {
            // Sequence numbers less than zero are used for control messages.
            if (sequenceNumber < 0)
            {
                return;
            }

            bool removed = false;
            RequestState state;
            lock (_lock)
            {
                if (afterResponseNumber >= 0)
                {
                    if (_outboundRequests.TryGetValue(afterResponseNumber, out var dependsOn))
                    {
                        // Register a dependency.
                        if (dependsOn.Dependent != -1)
                        {
                            throw new InvalidOperationException($"Response {afterResponseNumber} is already set to signal response {dependsOn.Dependent} and cannot signal response {sequenceNumber}");
                        }

                        dependsOn.Dependent = sequenceNumber;
                        _outboundRequests[afterResponseNumber] = dependsOn;

                        state = new RequestState
                        {
                            Response = response,
                            Dependent = -1,
                            DependsOn = afterResponseNumber,
                        };
                        _outboundRequests[sequenceNumber] = state;
                    }
                    else
                    {
                        // The dependency must have already been satisfied.
                        _outboundRequests.TryGetValue(sequenceNumber, out state);
                        state.Response = response;
                        state.Dependent = -1;
                        state.DependsOn = -1;
                        _outboundRequests[sequenceNumber] = state;
                    }
                }
                else
                {
                    removed = _outboundRequests.Remove(sequenceNumber, out state);
                    state.Response = response;

                    if (removed && (state.Completion is null || state.DependsOn != -1))
                    {
                        // Add it back with the response value set, since it is not ready to be executed yet.
                        _outboundRequests.Add(sequenceNumber, state);
                        removed = false;
                    }
                    else if (!removed)
                    {
                        // Register the response for later execution, when the request is made.
                        _outboundRequests[sequenceNumber] = new RequestState
                        {
                            Response = response,
                            Dependent = -1,
                            DependsOn = -1,
                        };
                    }
                }
            }

            if (removed)
            {
                CompleteRequest(this, state);
            }
        }

        private static void TryCompleteRequest(WorkflowContext context, long sequenceNumber)
        {
            if (context._outboundRequests.Remove(sequenceNumber, out var state))
            {
                if (state.Completion is object)
                {
                    CompleteRequest(context, state);
                }
                else
                {
                    state.DependsOn = -1;
                    context._outboundRequests[sequenceNumber] = state;
                }
            }
            else
            {
                context._log.LogInformation("`sdafsfsadfasd");
            }
        }

        private static void CompleteRequest(WorkflowContext context, RequestState state)
        {
            state.Completion.Complete(state.Response);
            if (state.Dependent >= 0)
            {
                TryCompleteRequest(context, state.Dependent);
            }
        }

        public void OnCommittedRequest(Message message)
        {
            lock (_lock)
            {
                if (_currentMessage is null)
                {
                    // Synchronously start invocation under the lock.
                    Invoke(message);
                }
                else
                {
                    // Enqueue the message. The current invocation will complete by acquiring the lock and checking the queue.
                    _messageQueue.Enqueue(message);
                }
            }

            void RunMessagePump()
            {
                while (true)
                {
                    lock (_lock)
                    {
                        if (!_messageQueue.TryPeek(out var message))
                        {
                            return;
                        }

                        if (_currentMessage is null)
                        {
                            // Invoke the message.
                            // No need to observe the dequeued message, since we are under the same lock it was peeked on.
                            _ = _messageQueue.Dequeue();
                            Invoke(message);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            // Note: this method must not be 'async' itself. It must be called under the lock, and _currentMessage
            // must be accessed under the lock.
            void Invoke(Message message)
            {
                var request = (IInvokable)message.Body;
                request.SetTarget(_targetHolder);

                ValueTask<Response> responseTask;
                try
                {
                    _currentMessage = message;
                    responseTask = request.Invoke();
                }
                catch (Exception exception)
                {
                    responseTask = new ValueTask<Response>(Response.FromException<object>(exception));
                }

                if (responseTask.IsCompleted)
                {
                    _router.SendMessage(message.SenderId, new Message { SenderId = _id, SequenceNumber = message.SequenceNumber, Body = responseTask.Result });

                    _currentMessage = null;
                    if (_messageQueue.Count > 0)
                    {
                        RunMessagePump();
                    }
                }
                else
                {
                    _ = HandleRequestAsync(message, responseTask);

                    async ValueTask HandleRequestAsync(Message message, ValueTask<Response> task)
                    {
                        Response response = default;
                        try
                        {
                            response = await task;
                        }
                        catch (Exception exception)
                        {
                            response = Response.FromException<object>(exception);
                        }
                        finally
                        {
                            _router.SendMessage(message.SenderId, new Message { SenderId = _id, SequenceNumber = message.SequenceNumber, Body = response });

                            lock (_lock)
                            {
                                _currentMessage = null;
                                if (_messageQueue.Count > 0)
                                {
                                    RunMessagePump();
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnMessage(object message)
        {
            // If message has not been committed, commit it.
            //   How do we know?
            //      If it is a request, it must not have been committed.
            //      If it is a response && we are waiting for it, it must not have been committed
            //         - NOTE: this implies we pre-populate the _callback dictionary with all committed responses 
            // If message is response, execute it.
            // else if message is request && no current request is executing, schedule it.
            // else if message is request && a current request is executing, enqueue it.

            if (message is Message msg && msg.SequenceNumber < 0)
            {
                return;
            }

            if (!_logStage.Writer.TryWrite(message))
            {
                _log.LogWarning("Received message {Message} after deactivation has begun", message);
            }
        }

        /// <summary>
        /// Returns false if a request has already been seen by this workflow.
        /// </summary>
        /// <param name="completion"></param>
        /// <param name="sequenceNumber"></param>
        /// <returns></returns>
        public bool OnCreateRequest(IResponseCompletionSource completion, out long sequenceNumber)
        {
            var previousContext = RuntimeContext.Current;
            RuntimeContext.Current = this;
            try
            {
                bool added;
                RequestState state;
                lock (_lock)
                {
                    // Get a unique id for this request.
                    var messageId = ++_sequenceNumber;
                    sequenceNumber = messageId;
                    state = new RequestState { Completion = completion, Dependent = -1, DependsOn = -1 };
                    if (_outboundRequests.TryAdd(messageId, state))
                    {
                        added = true;
                        state = default;
                    }
                    else
                    {
                        // This request has already been sent and a reponse has been received.
                        // Avoid sending the request again. The response will be replayed during recovery to unblock the request.
                        added = false;
                        var removed = _outboundRequests.Remove(messageId, out state);
                        if (state.Completion is object)
                        {
                            throw new InvalidOperationException($"Duplicate request for sequence number {sequenceNumber}");
                        }

                        state.Completion = completion;
                        Debug.Assert(removed);
                    }
                }

                if (!added && state.DependsOn == -1)
                {
                    CompleteRequest(this, state);
                }

                return added;
            }
            finally
            {
                RuntimeContext.Current = previousContext;
            }
        }

        private async Task RunLogWriter()
        {
            RuntimeContext.Current = this;
            var writeQueue = new List<(long logAddress, object)>();
            var reader = _logStage.Reader;
            long replayAfter = 0L;
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var item))
                {
                    // Avoid writing already-written responses.
                    // TODO: Ideally, this should also be modified de-duplicate requests from other workflows
                    if (item is Message msg && msg.Body is Response)
                    {
                        lock (_lock)
                        {
                            replayAfter = _sequenceNumber;
                            if (msg.SequenceNumber <= _sequenceNumber
                                && (!_outboundRequests.TryGetValue(msg.SequenceNumber, out var pending) || pending.Response is object || pending.Completion is null))
                            {
                                if (_log.IsEnabled(LogLevel.Trace))
                                {
                                    _log.LogTrace("... {Id} skipping already-seen response {Message}", _id.ToString(), msg);
                                }

                                continue;
                            }
                        }
                    }

                    // TODO: Record the sequence number so that this response is replayed once that sequence number has been reached.
                    // This allows for deterministic replay with interleaving requests.

                    var address = _logManager.EnqueueLogEntry(_id, item);
                    writeQueue.Add((address, item));
                }

                foreach (var (address, message) in writeQueue)
                {
                    await _logManager.WaitForCommitAsync(address, CancellationToken.None);
                    OnCommittedMessage(message);
                }

                writeQueue.Clear();
            }
        }

        public ValueTask CancelRunningRequests()
        {
            var allRequests = _outboundRequests.ToList();
            var cancelledRequest = new List<long>();
            for (int i = 0; i < allRequests.Count; ++i)
            {
                var request = allRequests[i];
                var (sequenceNumber, state) = (request.Key, request.Value);
                if (state.Response is null && state.Completion is object)
                {
                    state.Response = Response.FromException<object>(new TaskCanceledException("Running requests have been canceled"));
                }

                _outboundRequests[sequenceNumber] = state;
                cancelledRequest.Add(sequenceNumber);
            }

            foreach (var sequenceNumber in cancelledRequest)
            {
                TryCompleteRequest(this, sequenceNumber);
            }

            return default;
        }

        private readonly struct TargetHolder : ITargetHolder
        {
            private readonly WorkflowContext _context;

            public TargetHolder(WorkflowContext context) => _context = context;

            public TTarget GetTarget<TTarget>() => (TTarget)_context._instance;

            public TComponent GetComponent<TComponent>()
            {
                if (_context is TComponent contextComponent)
                {
                    return contextComponent;
                }

                if (_context._instance is TComponent instanceComponent)
                {
                    return instanceComponent;
                }

                throw new InvalidOperationException($"Extension {typeof(TComponent)} not available");
            }
        }
    }
}
