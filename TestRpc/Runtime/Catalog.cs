using System.Collections.Concurrent;

namespace TestRpc.Runtime
{
    internal class Catalog
    {
        private readonly ConcurrentDictionary<GrainId, Activation> activations = new ConcurrentDictionary<GrainId, Activation>(GrainIdEqualityComparer.Instance);
        public void RegisterActivation(Activation target) => this.activations[target.GrainId] = target;
        public Activation GetActivation(GrainId id) => this.activations[id];
    }
}