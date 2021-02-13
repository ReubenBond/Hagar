using Hagar;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CallLog
{
    public interface ICounterWorkflow : IWorkflow
    {
        [Id(1)]
        ValueTask<int> Increment();

        [Id(2)]
        ValueTask<DateTime> PingPongFriend(IdSpan friend, int cycles);
        
        [Id(3)]
        ValueTask<int> GetCounter();
    }

    internal interface IHasWorkflowContext
    {
        WorkflowContext Context { get; set; }
    }

    public class CounterWorkflow : ICounterWorkflow, IHasWorkflowContext
    {
        private int _msgId;

        private int _counter;

        [NonSerialized]
        private readonly IdSpan _id;

        [NonSerialized]
        private readonly ILogger<CounterWorkflow> _log;

        [NonSerialized]
        private readonly ProxyFactory _proxyFactory;

        WorkflowContext IHasWorkflowContext.Context
        {
            get => _context;
            set
            {
                _context = value;
                _context.MaxSupportedVersion = 3;
            }
        }

        private WorkflowContext _context;

        public CounterWorkflow(IdSpan id, ILogger<CounterWorkflow> log, ProxyFactory proxyFactory)
        {
            _msgId = 0;
            _id = id;
            _log = log;
            _proxyFactory = proxyFactory;
        }

        // Queries do not need special handling.
        public ValueTask<int> GetCounter() => new ValueTask<int>(_counter);

        public async ValueTask<int> Increment()
        {
            if (_context.CurrentVersion >= 3)
            {
                // One way of handling versioning is to check the CurrentVersion
                // property. That way, changes to logic can be made in a manner which doesn't
                // break old histories
                _log.LogInformation("Sleeping for a sec");
                await WorkflowAction.Delay(TimeSpan.FromSeconds(1));
            }

            _log.LogInformation("{Id}.{MsgId} Incrementing counter: {Counter}", _id.ToString(), _msgId++.ToString(), _counter);
            return ++_counter;
        }

        public async ValueTask<DateTime> PingPongFriend(IdSpan friend, int cycles)
        {
            if (cycles <= 0)
            {
                // Anything non-deterministic needs to be encapsulated and recorded
                // for future playback
                var time = await WorkflowAction.GetUtcNow();
                _log.LogInformation("{Id}.{MsgId} says PINGPONG FRIEND at {DateTime}!", _id.ToString(), _msgId++.ToString(), time);
                return time;
            }
            else
            {
                // Similar to Orleans grains, Workflows are virtual and have a managed lifecycle.
                // Therefore the "just start calling it" pattern from Orleans applies
                var friendProxy = _proxyFactory.GetProxy<ICounterWorkflow, WorkflowProxyBase>(friend);
                var time = await friendProxy.PingPongFriend(_id, cycles - 1);
                _log.LogInformation("{Id}.{MsgId} received PINGPONG FRIEND at {DateTime}!", _id.ToString(), _msgId++.ToString(), time);

                return time;
            }
        }
    }
}
