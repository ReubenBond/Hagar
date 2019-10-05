using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Hagar.Invocation
{
    public sealed class ResponseCompletionSource<TResult> : IResponseCompletionSource, IValueTaskSource<TResult>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<TResult> _core = new ManualResetValueTaskSourceCore<TResult>();

        public ValueTask<TResult> AsValueTask() => new ValueTask<TResult>(this, _core.Version);

        public ValueTask AsVoidValueTask() => new ValueTask(this, _core.Version);

        public TResult GetResult(short token)
        {
            var result = _core.GetResult(token);
            this.Reset();
            return result;
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _core.OnCompleted(continuation, state, token, flags);
        }

        public void Reset()
        {
            _core.Reset();
            ResponseCompletionSourcePool.Return(this);
        }

        public void SetException(Exception exception) => _core.SetException(exception);

        public void SetResult(TResult result) => _core.SetResult(result);

        public void Complete(Response value)
        {
            if (value is Response<TResult> typed)
            {
                this.Complete(typed);
            }
            else if (value.Exception is Exception exception)
            {
                this.SetException(exception);
            }
            else
            {
                this.SetResult((TResult)value.Result);
            }
        }

        /// <summary>
        /// Sets the result.
        /// </summary>
        /// <param name="value">The result value.</param>
        public void Complete(Response<TResult> value)
        {
            if (value.Exception is Exception exception)
            {
                this.SetException(exception);
            }
            else
            {
                this.SetResult(value.TypedResult);
            }
        }

        void IValueTaskSource.GetResult(short token)
        {
            _core.GetResult(token);
            this.Reset();
        }
    }
}