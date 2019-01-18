using System.Collections.Concurrent;

namespace TestRpc.Runtime
{
    internal class Catalog
    {
        private readonly ConcurrentDictionary<ActivationId, Activation> activations = new ConcurrentDictionary<ActivationId, Activation>();
        public void RegisterActivation(Activation target) => this.activations[target.ActivationId] = target;
        public Activation GetActivation(ActivationId id) => this.activations[id];
    }
}