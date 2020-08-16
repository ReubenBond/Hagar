namespace Hagar.Configuration
{
    /// <summary>
    /// Provides configuration of the specified type.
    /// </summary>
    /// <typeparam name="TConfiguration">The configuration type.</typeparam>
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IConfigurationProvider<TConfiguration>
    {
        /// <summary>
        /// Populates the provided configuration object.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void Configure(TConfiguration configuration);
    }
}