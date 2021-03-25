using Hagar;
using Hagar.Invocation;
using System;
using System.Threading.Tasks;

namespace TestRpc.Runtime
{
    [DefaultInvokableBaseType(typeof(ValueTask<>), typeof(Request<>))]
    [DefaultInvokableBaseType(typeof(ValueTask), typeof(Request))]
    [DefaultInvokableBaseType(typeof(Task<>), typeof(TaskRequest<>))]
    [DefaultInvokableBaseType(typeof(Task), typeof(TaskRequest))]
    [DefaultInvokableBaseType(typeof(void), typeof(VoidRequest))]
    public abstract class ProxyBase
    {
        [NonSerialized]
        private readonly IRuntimeClient _runtimeClient;

        protected ProxyBase(GrainId id, IRuntimeClient runtimeClient)
        {
            _runtimeClient = runtimeClient;
            GrainId = id;
        }

        public GrainId GrainId { get; }

        protected void SendRequest(IResponseCompletionSource callback, IInvokable body) => _runtimeClient.SendRequest(GrainId, callback, body);
    }
}