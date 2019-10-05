using System;

namespace Hagar.Invocation
{
    public class PooledResponse<TResult> : Response<TResult>
    {
        public override TResult TypedResult { get; set; }

        public override Exception Exception { get; set; }

        public override object Result
        {
            get => this.TypedResult;
            set => this.TypedResult = (TResult)value;
        }

        public override void Dispose()
        {
            this.TypedResult = default;
            this.Exception = default;
            ResponsePool.Return(this);
        }
    }
}