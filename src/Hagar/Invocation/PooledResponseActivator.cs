using Hagar.Activators;

namespace Hagar.Invocation
{
    [RegisterActivator]
    internal sealed class PooledResponseActivator<TResult> : IActivator<PooledResponse<TResult>>
    {
        public PooledResponse<TResult> Create() => ResponsePool.Get<TResult>();
    }
}