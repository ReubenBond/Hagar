using System;

namespace Hagar.Invocation
{
    [GenerateSerializer]
    [Immutable]
    [SuppressReferenceTracking]
    public class PooledResponse<TResult> : Response<TResult>
    {
        [Id(0)]
        public override TResult TypedResult { get; set; }

        public override Exception Exception
        {
            get => null;
            set => throw new InvalidOperationException($"Cannot set {nameof(Exception)} property for type {nameof(Response<TResult>)}");
        }

        public override object Result
        {
            get => TypedResult;
            set => TypedResult = (TResult)value;
        }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public override void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            TypedResult = default;
            ResponsePool.Return(this);
        }
    }
}