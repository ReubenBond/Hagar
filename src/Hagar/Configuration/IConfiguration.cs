namespace Hagar.Configuration
{
    /// <summary>
    /// Holds configuration of the specified type.
    /// </summary>
    /// <typeparam name="TConfiguration">The configuration  type.</typeparam>
    public interface IConfiguration<TConfiguration> where TConfiguration : class, new()
    {
        /// <summary>
        /// Gets the configuration value.
        /// </summary>
        TConfiguration Value { get; }
    }
}