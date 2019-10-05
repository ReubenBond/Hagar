using System;
using System.Threading.Tasks;

namespace Hagar.Invocation
{

    /// <summary>
    /// Represents an object which can be invoked asynchronously.
    /// </summary>
    public interface IInvokable : IDisposable
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
        ValueTask<Response> Invoke();

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
    }
}