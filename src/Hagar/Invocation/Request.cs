using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Hagar.Invocation
{
    public abstract class Request : IInvokable
    {
        public abstract int ArgumentCount { get; }

        [DebuggerHidden]
        public ValueTask<Response> Invoke()
        {
            try
            {
                var resultTask = InvokeInner();
                if (resultTask.IsCompleted)
                {
                    resultTask.GetAwaiter().GetResult();
                    return new ValueTask<Response>(Response.FromResult<object>(null));
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException<object>(exception));
            }
        }

        [DebuggerHidden]
        private async ValueTask<Response> CompleteInvokeAsync(ValueTask resultTask)
        {
            try
            {
                await resultTask;
                return Response.FromResult<object>(null);
            }
            catch (Exception exception)
            {
                return Response.FromException<object>(exception);
            }
        }

        // Generated
        [DebuggerHidden]
        protected abstract ValueTask InvokeInner();
        public abstract TTarget GetTarget<TTarget>();
        public abstract void SetTarget<TTargetHolder>(TTargetHolder holder) where TTargetHolder : ITargetHolder;
        public abstract TArgument GetArgument<TArgument>(int index);
        public abstract void SetArgument<TArgument>(int index, in TArgument value);
        public abstract void Dispose();
    }

    public abstract class Request<TResult> : IInvokable
    {
        public abstract int ArgumentCount { get; }

        [DebuggerHidden]
        public ValueTask<Response> Invoke()
        {
            try
            {
                var resultTask = InvokeInner();
                if (resultTask.IsCompleted)
                {
                    return new ValueTask<Response>(Response.FromResult(resultTask.Result));
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException<TResult>(exception));
            }
        }

        [DebuggerHidden]
        private async ValueTask<Response> CompleteInvokeAsync(ValueTask<TResult> resultTask)
        {
            try
            {
                var result = await resultTask;
                return Response.FromResult(result);
            }
            catch (Exception exception)
            {
                return Response.FromException<TResult>(exception);
            }
        }

        // Generated
        [DebuggerHidden]
        protected abstract ValueTask<TResult> InvokeInner();
        public abstract TTarget GetTarget<TTarget>();
        public abstract void SetTarget<TTargetHolder>(TTargetHolder holder) where TTargetHolder : ITargetHolder;
        public abstract TArgument GetArgument<TArgument>(int index);
        public abstract void SetArgument<TArgument>(int index, in TArgument value);
        public abstract void Dispose();
    }

    public abstract class TaskRequest<TResult> : IInvokable
    {
        public abstract int ArgumentCount { get; }

        [DebuggerHidden]
        public ValueTask<Response> Invoke()
        {
            try
            {
                var resultTask = InvokeInner();
                var status = resultTask.Status;
                if (resultTask.IsCompleted)
                {
                    return new ValueTask<Response>(Response.FromResult(resultTask.GetAwaiter().GetResult()));
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException<TResult>(exception));
            }
        }

        [DebuggerHidden]
        private async ValueTask<Response> CompleteInvokeAsync(Task<TResult> resultTask)
        {
            try
            {
                var result = await resultTask;
                return Response.FromResult(result);
            }
            catch (Exception exception)
            {
                return Response.FromException<TResult>(exception);
            }
        }

        // Generated
        [DebuggerHidden]
        protected abstract Task<TResult> InvokeInner();
        public abstract TTarget GetTarget<TTarget>();
        public abstract void SetTarget<TTargetHolder>(TTargetHolder holder) where TTargetHolder : ITargetHolder;
        public abstract TArgument GetArgument<TArgument>(int index);
        public abstract void SetArgument<TArgument>(int index, in TArgument value);
        public abstract void Dispose();
    }

    public abstract class TaskRequest : IInvokable
    {
        public abstract int ArgumentCount { get; }

        [DebuggerHidden]
        public ValueTask<Response> Invoke()
        {
            try
            {
                var resultTask = InvokeInner();
                var status = resultTask.Status;
                if (resultTask.IsCompleted)
                {
                    resultTask.GetAwaiter().GetResult();
                    return new ValueTask<Response>(Response.FromResult<object>(null));
                }

                return CompleteInvokeAsync(resultTask);
            }
            catch (Exception exception)
            {
                return new ValueTask<Response>(Response.FromException<object>(exception));
            }
        }

        [DebuggerHidden]
        private async ValueTask<Response> CompleteInvokeAsync(Task resultTask)
        {
            try
            {
                await resultTask;
                return Response.FromResult<object>(null);
            }
            catch (Exception exception)
            {
                return Response.FromException<object>(exception);
            }
        }

        // Generated
        [DebuggerHidden]
        protected abstract Task InvokeInner();
        public abstract TTarget GetTarget<TTarget>();
        public abstract void SetTarget<TTargetHolder>(TTargetHolder holder) where TTargetHolder : ITargetHolder;
        public abstract TArgument GetArgument<TArgument>(int index);
        public abstract void SetArgument<TArgument>(int index, in TArgument value);
        public abstract void Dispose();
    }
}