using System;

namespace Hagar.Invocation
{
    public class PooledResponse<TResult> : Response<TResult>
    {
        public override TResult TypedResult { get; set; }

        public override Exception Exception { get; set; }

        public override object Result
        {
            get => TypedResult;
            set => TypedResult = (TResult)value;
        }

        public override void Dispose()
        {
            TypedResult = default;
            Exception = default;
            ResponsePool.Return(this);
        }
    }
}