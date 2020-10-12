using CallLog.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CallLog.Scheduling
{
    /// <summary>
    /// A single-concurrency, in-order task scheduler for per-activation work scheduling.
    /// </summary>
    [DebuggerDisplay("ActivationTaskScheduler-{_myId} RunQueue={_workItems.Count}")]
    internal class ActivationTaskScheduler : TaskScheduler
#if NETCOREAPP
        , IThreadPoolWorkItem
#endif
    {
        private static readonly WaitCallback ExecuteWorkItemCallback = obj => ((ActivationTaskScheduler)obj).Execute();
        private static long IdCounter;

        private readonly long _myId;
        private readonly ILogger _log;
        private readonly object _lockable;
        private readonly Queue<Task> _workItems;
        private readonly CancellationToken _cancellationToken;
        private readonly IWorkflowContext _context;
        private Status _state;

        private enum Status
        {
            Waiting = 0,
            Runnable = 1,
            Running = 2
        }

        public ActivationTaskScheduler(
            IWorkflowContext context,
            CancellationToken ct,
            ILogger<ActivationTaskScheduler> logger)
        {
            _myId = Interlocked.Increment(ref IdCounter);
            _context = context;
            _cancellationToken = ct;
            _state = Status.Waiting;
            _workItems = new Queue<Task>();
            _lockable = new object();
            _log = logger;
        }

        /// <summary>Queues a task to the scheduler.</summary>
        /// <param name="task">The task to be queued.</param>
        public void EnqueueTask(Task task) => QueueTask(task);

        /// <summary>
        /// Determines whether the provided <see cref="T:System.Threading.Tasks.Task"/> can be executed synchronously in this call, and if it can, executes it.
        /// </summary>
        /// <returns>
        /// A Boolean value indicating whether the task was executed inline.
        /// </returns>
        /// <param name="task">The <see cref="T:System.Threading.Tasks.Task"/> to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">A Boolean denoting whether or not task has previously been queued. If this parameter is True, then the task may have been previously queued (scheduled); if False, then the task is known not to have been queued, and this call is being made in order to execute the task inline without queuing it.</param>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            var ctx = RuntimeContext.Current;
            bool canExecuteInline = ctx != null && object.Equals(ctx, _context);

            if (!canExecuteInline)
            {
                return false;
            }

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued)
            {
                canExecuteInline = TryDequeue(task);
            }

            if (!canExecuteInline)
            {
                return false;
            }

            // Try to run the task.
            bool done = TryExecuteTask(task);
            return done;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}:Queued={2}", GetType().Name, _myId, ExternalWorkItemCount);
        }

        public int ExternalWorkItemCount
        {
            get { lock (_lockable) { return WorkItemCount; } }
        }

        private int WorkItemCount
        {
            get { return _workItems.Count; }
        }
 
        /// <summary>
        /// Adds a task to this activation.
        /// If we're adding it to the run list and we used to be waiting, now we're runnable.
        /// </summary>
        /// <param name="task">The work item to add.</param>
        protected override void QueueTask(Task task)
        {
#if DEBUG
            if (_log.IsEnabled(LogLevel.Trace))
            {
                _log.LogTrace(
                    "EnqueueWorkItem {Task} into {GrainContext} when TaskScheduler.Current={TaskScheduler}",
                    task,
                    _context,
                    System.Threading.Tasks.TaskScheduler.Current);
            }
#endif

            lock (_lockable)
            {
                int count = WorkItemCount;

                _workItems.Enqueue(task);
                if (_state != Status.Waiting)
                {
                    return;
                }

                _state = Status.Runnable;
                ScheduleExecution(this);
            }
        }

        /// <summary>
        /// For debugger purposes only.
        /// </summary>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            foreach (var task in _workItems)
            {
                yield return task;
            }
        }

        private static object DumpAsyncState(object o)
        {
            if (o is Delegate action)
            {
                return action.Target is null ? action.Method.DeclaringType + "." + action.Method.Name
                    : action.Method.DeclaringType.Name + "." + action.Method.Name + ": " + DumpAsyncState(action.Target);
            }

            if (o?.GetType() is { Name: "ContinuationWrapper" } wrapper
                && (wrapper.GetField("_continuation", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? wrapper.GetField("m_continuation", BindingFlags.Instance | BindingFlags.NonPublic)
                    )?.GetValue(o) is Action continuation)
            {
                return DumpAsyncState(continuation);
            }

#if !NETCOREAPP
            if (o?.GetType() is { Name: "MoveNextRunner" } runner
                && runner.GetField("m_stateMachine", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(o) is object stateMachine)
                return DumpAsyncState(stateMachine);
#endif

            return o;
        }

        // Execute one or more turns for this activation. 
        // This method is always called in a single-threaded environment -- that is, no more than one
        // thread will be in this method at once -- but other asynch threads may still be queueing tasks, etc.
        public void Execute()
        {
            try
            {
                RuntimeContext.Current = _context;

                int count = 0;
                var stopwatch = ValueStopwatch.StartNew();
                do
                {
                    lock (_lockable)
                    {
                        _state = Status.Running;

                        // Check the cancellation token (means that the silo is stopping)
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            _log.LogWarning(
                                "Thread {Thread} is exiting work loop due to cancellation token. TaskScheduler: {TaskScheduler}, Have {WorkItemCount} work items in the queue",
                                Thread.CurrentThread.ManagedThreadId.ToString(),
                                ToString(),
                                WorkItemCount);

                            return;
                        }
                    }

                    // Get the first Work Item on the list
                    Task task;
                    lock (_lockable)
                    {
                        if (_workItems.Count > 0)
                        {
                            task = _workItems.Dequeue();
                        }
                        else // If the list is empty, then we're done
                        {
                            break;
                        }
                    }

#if DEBUG
                    if (_log.IsEnabled(LogLevel.Trace))
                    {
                        _log.LogTrace(
                        "About to execute task {Task} in GrainContext={GrainContext}",
                        task,
                        _context);
                    }
#endif
                    var taskStart = stopwatch.Elapsed;

                    try
                    {
                        RuntimeContext.Current = _context;
                        TryExecuteTask(task);
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(
                            ex,
                            "Worker thread caught an exception thrown from Execute by task {Task}. Exception: {Exception}",
                            task,
                            ex);
                        throw;
                    }
                    finally
                    {
                        var taskLength = stopwatch.Elapsed - taskStart;
                        if (taskLength > TimeSpan.FromSeconds(1))
                        {
                            _log.LogDebug(
                                "Task {Task} in WorkGroup {GrainContext} took elapsed time {Duration} for execution, which is longer than {TurnWarningLengthThreshold}. Running on thread {Thread}",
                                task,
                                _context.ToString(),
                                taskLength.ToString("g"),
                                TimeSpan.FromSeconds(1),
                                Thread.CurrentThread.ManagedThreadId.ToString());
                        }
                    }
                    count++;
                }
                while (true);
            }
            catch (Exception ex)
            {
                _log.LogError(
                    ex,
                    "Worker thread {Thread} caught an exception thrown from IWorkItem.Execute: {Exception}",
                    Thread.CurrentThread.ManagedThreadId,
                    ex);
            }
            finally
            {
                // Now we're not Running anymore. 
                // If we left work items on our run list, we're Runnable, and need to go back on the silo run queue; 
                // If our run list is empty, then we're waiting.
                lock (_lockable)
                {
                    if (WorkItemCount > 0)
                    {
                        _state = Status.Runnable;
                        ScheduleExecution(this);
                    }
                    else
                    {
                        _state = Status.Waiting;
                    }
                }

                RuntimeContext.Current = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScheduleExecution(ActivationTaskScheduler scheduler)
        {
#if NETCOREAPP
            ThreadPool.UnsafeQueueUserWorkItem(scheduler, preferLocal: true);
#else
            ThreadPool.UnsafeQueueUserWorkItem(ExecuteWorkItemCallback, scheduler);
#endif
        }
    }
}
