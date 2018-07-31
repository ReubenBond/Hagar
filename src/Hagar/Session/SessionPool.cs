using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace Hagar.Session
{
    public sealed class SessionPool
    {
        private readonly ObjectPool<SerializerSession> sessionPool;

        public SessionPool(IServiceProvider serviceProvider)
        {
            var sessionPoolPolicy = new SerializerSessionPoolPolicy(serviceProvider, this.ReturnSession);
            this.sessionPool = new DefaultObjectPool<SerializerSession>(sessionPoolPolicy);
        }

        public SerializerSession GetSession() => this.sessionPool.Get();

        private void ReturnSession(SerializerSession session) => this.sessionPool.Return(session);

        private class SerializerSessionPoolPolicy : IPooledObjectPolicy<SerializerSession>
        {
            private readonly IServiceProvider serviceProvider;
            private readonly ObjectFactory factory;
            private readonly Action<SerializerSession> onSessionDisposed;

            public SerializerSessionPoolPolicy(IServiceProvider serviceProvider, Action<SerializerSession> onSessionDisposed)
            {
                this.serviceProvider = serviceProvider;
                this.onSessionDisposed = onSessionDisposed;
                this.factory = ActivatorUtilities.CreateFactory(typeof(SerializerSession), new Type[0]);
            }

            public SerializerSession Create()
            {
                var result = (SerializerSession)this.factory(this.serviceProvider, null);
                result.OnDisposed = this.onSessionDisposed;
                return result;
            }

            public bool Return(SerializerSession obj)
            {
                obj.FullReset();
                return true;
            }
        }
    }
}