using System.Threading.Tasks;

namespace Hagar.Invocation
{
    /// <inheritdoc />
    public abstract class Invokable : IInvokable
    {
        /// <inheritdoc />
        public abstract TTarget GetTarget<TTarget>();

        /// <inheritdoc />
        public abstract void SetTarget<TTargetHolder>(TTargetHolder holder) where TTargetHolder : ITargetHolder;

        /// <inheritdoc />
        public abstract ValueTask Invoke();

        /// <inheritdoc />
        public object Result
        {
            get => this.GetResult<object>();
            set => this.SetResult(in value);
        }

        /// <inheritdoc />
        public abstract TResult GetResult<TResult>();

        /// <inheritdoc />
        public abstract void SetResult<TResult>(in TResult value);

        ///// <inheritdoc />
        //public abstract void SerializeResult<TBufferWriter>(ref Writer<TBufferWriter> writer) where TBufferWriter : IBufferWriter<byte>;

        /// <inheritdoc />
        public abstract int ArgumentCount { get; }

        /// <inheritdoc />
        public abstract TArgument GetArgument<TArgument>(int index);

        /// <inheritdoc />
        public abstract void SetArgument<TArgument>(int index, in TArgument value);

        /// <inheritdoc />
        public abstract void Reset();
    }
}