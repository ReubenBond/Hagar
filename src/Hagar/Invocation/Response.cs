using System;
using System.Runtime.ExceptionServices;

namespace Hagar.Invocation
{
    [GenerateSerializer]
    public abstract class Response : IDisposable
    {
        public static Response FromException(Exception exception) => new ExceptionResponse { Exception = exception };

        public static Response FromResult<TResult>(TResult value)
        {
            var result = ResponsePool.Get<TResult>();
            result.TypedResult = value;
            return result;
        }

        public static Response Completed { get; } = new CompletedResponse();

        public abstract object Result { get; set; }

        public abstract Exception Exception { get; set; }

        public abstract void Dispose();
    }

    [GenerateSerializer]
    public sealed class CompletedResponse : Response
    {
        public override object Result { get => null; set => throw new InvalidOperationException($"Type {nameof(CompletedResponse)} is read-only"); } 

        public override Exception Exception { get => null; set => throw new InvalidOperationException($"Type {nameof(CompletedResponse)} is read-only"); } 

        public override void Dispose() { }
    }

    [GenerateSerializer]
    public sealed class ExceptionResponse : Response
    {
        public override object Result
        {
            get
            {
                ExceptionDispatchInfo.Capture(Exception).Throw();
                return null;
            }

            set => throw new InvalidOperationException($"Cannot set result property on response of type {nameof(ExceptionResponse)}");
        }

        [Id(0)]
        public override Exception Exception { get; set; }

        public override void Dispose() { }
    }

    [GenerateSerializer]
    public abstract class Response<TResult> : Response
    {
        public abstract TResult TypedResult { get; set; }
    }
}