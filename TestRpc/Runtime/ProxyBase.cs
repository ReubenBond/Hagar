using System;
using Hagar.Invocation;

namespace TestRpc.Runtime
{
    public abstract class ProxyBase
    {
        [NonSerialized]
        private readonly IRuntimeClient runtimeClient;

        protected ProxyBase(ActivationId id, IRuntimeClient runtimeClient)
        {
            this.runtimeClient = runtimeClient;
            this.ActivationId = id;
        }

        public ActivationId ActivationId { get; }

        protected void SendRequest(IResponseCompletionSource callback, IInvokable body) => this.runtimeClient.SendRequest(this.ActivationId, callback, body);
    }
}