using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Hagar.Invocation
{
    public sealed class ResponseCompletionSource<TResult> : IResponseCompletionSource, IValueTaskSource<TResult>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<TResult> _core;

        public ValueTask<TResult> AsValueTask() => new ValueTask<TResult>(this, _core.Version);

        public ValueTask AsVoidValueTask() => new ValueTask(this, _core.Version);

        public TResult GetResult(short token)
        {
            var result = _core.GetResult(token);
            Reset();
            return result;
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);

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
                Complete(typed);
            }
            else if (value.Exception is { } exception)
            {
                SetException(exception);
            }
            else
            {
                var result = value.Result;
                if (result is null)
                {
                    SetResult(default);
                }
                else
                {
                    SetResult((TResult)result);
                }
            }
        }

        /// <summary>
        /// Sets the result.
        /// </summary>
        /// <param name="value">The result value.</param>
        public void Complete(Response<TResult> value)
        {
            if (value.Exception is { } exception)
            {
                SetException(exception);
            }
            else
            {
                SetResult(value.TypedResult);
            }
        }

        public void Complete() => SetResult(default);

        void IValueTaskSource.GetResult(short token)
        {
            _ = _core.GetResult(token);
            Reset();
        }
    }
}