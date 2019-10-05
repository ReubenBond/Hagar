using Microsoft.Extensions.ObjectPool;

namespace Hagar.Invocation
{
    public static class ResponseCompletionSourcePool
    {
        public static ResponseCompletionSource<T> Get<T>() => TypedPool<T>.Pool.Get();

        public static void Return<T>(ResponseCompletionSource<T> obj) => TypedPool<T>.Pool.Return(obj);

        private static class TypedPool<T>
        {
            public static readonly DefaultObjectPool<ResponseCompletionSource<T>> Pool = new DefaultObjectPool<ResponseCompletionSource<T>>(new DefaultPooledObjectPolicy<ResponseCompletionSource<T>>());
        }
    }
}