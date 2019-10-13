using Microsoft.Extensions.ObjectPool;

namespace TestRpc.Runtime
{
    internal static class MessagePool
    {
        private static readonly DefaultObjectPool<Message> Pool = new DefaultObjectPool<Message>(new DefaultPooledObjectPolicy<Message>());

        public static Message Get() => Pool.Get();

        public static void Return(Message message) => Pool.Return(message);
    }
}