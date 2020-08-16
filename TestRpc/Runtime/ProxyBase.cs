using Hagar.Invocation;
using System;

namespace TestRpc.Runtime
{
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