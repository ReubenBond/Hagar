using System;
using Hagar.Invocation;

namespace TestRpc.Runtime
{
    public abstract class ProxyBase
    {
        [NonSerialized]
        private readonly IRuntimeClient runtimeClient;

        protected ProxyBase(GrainId id, IRuntimeClient runtimeClient)
        {
            this.runtimeClient = runtimeClient;
            this.GrainId = id;
        }

        public GrainId GrainId { get; }

        protected void SendRequest(IResponseCompletionSource callback, IInvokable body) => this.runtimeClient.SendRequest(this.GrainId, callback, body);
    }
}