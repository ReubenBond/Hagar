using Hagar.Activators;

namespace Hagar.Invocation
{
    internal sealed class PooledResponseActivator<TResult> : IActivator<PooledResponse<TResult>>
    {
        public PooledResponse<TResult> Create() => ResponsePool.Get<TResult>();
    }
}