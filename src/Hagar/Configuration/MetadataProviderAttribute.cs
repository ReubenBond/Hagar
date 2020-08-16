using System;
using System.Linq;

namespace Hagar.Configuration
{
    /// <summary>
    /// Defines a metadata provider for this assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class MetadataProviderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataProviderAttribute"/> class.
        /// </summary>
        /// <param name="providerType">The metadata provider type.</param>
        public MetadataProviderAttribute(Type providerType)
        {
            if (providerType is null)
            {
                throw new ArgumentNullException(nameof(providerType));
            }

            if (!providerType.GetInterfaces().Any(iface => iface.IsConstructedGenericType && typeof(IConfigurationProvider<>).IsAssignableFrom(iface.GetGenericTypeDefinition())))
            {
                throw new ArgumentException($"Provided type {providerType} must implement {typeof(IConfigurationProvider<>)}", nameof(providerType));
            }

            ProviderType = providerType;
        }

        /// <summary>
        /// Gets the metadata provider type.
        /// </summary>
        public Type ProviderType { get; }
    }
}