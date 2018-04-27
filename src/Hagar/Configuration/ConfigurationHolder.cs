using System.Collections.Generic;

namespace Hagar.Configuration
{
    /// <inheritdoc />
    internal class ConfigurationHolder<TConfiguration> : IConfiguration<TConfiguration> where TConfiguration : class, new()
    {
        /// <inheritdoc />
        public ConfigurationHolder(IEnumerable<IConfigurationProvider<TConfiguration>> providers)
        {
            this.Value = new TConfiguration();
            foreach (var provider in providers)
            {
                provider.Configure(this.Value);
            }
        }

        /// <inheritdoc />
        public TConfiguration Value { get; }
    }
}