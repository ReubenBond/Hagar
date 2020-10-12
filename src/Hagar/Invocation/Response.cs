using System;

namespace Hagar.Invocation
{
    [GenerateSerializer]
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

        public static Response Completed => new SuccessResponse();

        [Id(1)]
        public abstract object Result { get; set; }

        [Id(2)]
        public abstract Exception Exception { get; set; }

        public abstract void Dispose();
    }

    [GenerateSerializer]
    public sealed class SuccessResponse : Response
    {
        public override object Result { get; set; } 
        public override Exception Exception { get; set; }

        public override void Dispose() { }
    }

    public abstract class Response<TResult> : Response
    {
        public abstract TResult TypedResult { get; set; }
    }
}