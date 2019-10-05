using System;

namespace Hagar.Invocation
{
    public abstract class Response : IDisposable
    {
        public static Response FromException<TResult>(Exception exception)
        {
            var result = ResponsePool.Get<TResult>();
            result.Exception = exception;
            return result;
        }

        public static Response FromResult<TResult>(TResult value)
        {
            var result = ResponsePool.Get<TResult>();
            result.TypedResult = value;
            return result;
        }

        public abstract Exception Exception { get; set; }

        public abstract object Result { get; set; }

        public abstract void Dispose();
    }

    public abstract class Response<TResult> : Response
    {
        public abstract TResult TypedResult { get; set; }
    }
}