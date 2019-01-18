using System.Threading.Tasks;

namespace Hagar.Invocation
{
    /// <summary>
    /// Represents an object which can be invoked asynchronously.
    /// </summary>
    public interface IInvokable
    {
        /// <summary>
        /// Gets the invocation target.
        /// </summary>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <returns>The invocation target.</returns>
        TTarget GetTarget<TTarget>();

        /// <summary>
        /// Sets the invocation target from an instance of <see cref="ITargetHolder"/>.
        /// </summary>
        /// <typeparam name="TTargetHolder">The target holder type.</typeparam>
        /// <param name="holder">The invocation target.</param>
        void SetTarget<TTargetHolder>(TTargetHolder holder) where TTargetHolder : ITargetHolder;

        /// <summary>
        /// Invoke the object.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> which will complete when the invocation is complete.</returns>
        ValueTask Invoke();

        /// <summary>
        /// Gets or sets the result of invocation.
        /// </summary>
        /// <remarks>This property is internally set by <see cref="Invoke"/>.</remarks>
        object Result { get; set; }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>The result.</returns>
        TResult GetResult<TResult>();

        /// <summary>
        /// Sets the result.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="value">The result value.</param>
        void SetResult<TResult>(in TResult value);

        /// <summary>
        /// Serializes the result to the provided <paramref name="writer"/>.
        /// </summary>
        /// <typeparam name="TBufferWriter">The underlying buffer writer type.</typeparam>
        /// <param name="writer">The buffer writer.</param>
        //void SerializeResult<TBufferWriter>(ref Writer<TBufferWriter> writer) where TBufferWriter : IBufferWriter<byte>;

        /// <summary>
        /// Gets the number of arguments.
        /// </summary>
        int ArgumentCount { get; }

        /// <summary>
        /// Gets the argument at the specified index.
        /// </summary>
        /// <typeparam name="TArgument">The argument type.</typeparam>
        /// <param name="index">The argument index.</param>
        /// <returns>The argument at the specified index.</returns>
        TArgument GetArgument<TArgument>(int index);

        /// <summary>
        /// Sets the argument at the specified index.
        /// </summary>
        /// <typeparam name="TArgument">The argument type.</typeparam>
        /// <param name="index">The argument index.</param>
        /// <param name="value">The argument value</param>
        void SetArgument<TArgument>(int index, in TArgument value);

        /// <summary>
        /// Resets this instance.
        /// </summary>
        void Reset();
    }
}