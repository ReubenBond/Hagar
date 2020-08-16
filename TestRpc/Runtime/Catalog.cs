using System.Collections.Concurrent;

namespace TestRpc.Runtime
{
    internal class Catalog
    {
        private readonly ConcurrentDictionary<GrainId, Activation> _activations = new ConcurrentDictionary<GrainId, Activation>(GrainIdEqualityComparer.Instance);
        public void RegisterActivation(Activation target) => _activations[target.GrainId] = target;
        public Activation GetActivation(GrainId id) => _activations[id];
    }
}