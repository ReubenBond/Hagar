using System;
using System.Threading.Tasks;
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

        protected ValueTask Invoke<T>(T request) where T : IInvokable => this.runtimeClient.SendRequest(this.ActivationId, request);
    }
}