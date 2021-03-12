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

        [Id(1)]
        public override Exception Exception { get; set; }

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
            Exception = default;
            ResponsePool.Return(this);
        }
    }
}