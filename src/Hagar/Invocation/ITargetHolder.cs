namespace Hagar.Invocation
{
    /// <summary>
    /// Represents an object which holds an invocation target as well as target extensions.
    /// </summary>
    public interface ITargetHolder
    {
        /// <summary>
        /// Gets the target.
        /// </summary>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <returns>The target.</returns>
        TTarget GetTarget<TTarget>();

        /// <summary>
        /// Gets the extension object with the specified type.
        /// </summary>
        /// <typeparam name="TExtension">The extension type.</typeparam>
        /// <returns>The extension object with the specified type.</returns>
        TExtension GetExtension<TExtension>();
    }
}