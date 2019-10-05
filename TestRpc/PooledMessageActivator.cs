using Hagar.Activators;
using TestRpc.Runtime;

namespace TestRpc
{
    internal class PooledMessageActivator : IActivator<Message>
    {
        public Message Create() => MessagePool.Get();
    }
}