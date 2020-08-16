using Hagar.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Hagar
{
    public static class HagarBuilderExtensions
    {
        public static IHagarBuilder AddProvider(this IHagarBuilder builder, Func<IServiceProvider, IConfigurationProvider<SerializerConfiguration>> factory) => ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.AddTransient(sp => factory(sp)));

        public static IHagarBuilder AddProvider(this IHagarBuilder builder, IConfigurationProvider<SerializerConfiguration> provider) => ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.AddSingleton(provider));

        public static IHagarBuilder Configure(this IHagarBuilder builder, Action<SerializerConfiguration> configure) => builder.AddProvider(new DelegateConfigurationProvider<SerializerConfiguration>(configure));

        public static IHagarBuilder AddAssembly(this IHagarBuilder builder, Assembly assembly)
        {
            var attrs = assembly.GetCustomAttributes<MetadataProviderAttribute>();
            foreach (var attr in attrs)
            {
                if (!typeof(IConfigurationProvider<SerializerConfiguration>).IsAssignableFrom(attr.ProviderType))
                {
                    continue;
                }

                _ = builder.AddProvider(sp => (IConfigurationProvider<SerializerConfiguration>)ActivatorUtilities.GetServiceOrCreateInstance(sp, attr.ProviderType));
            }

            return builder;
        }

        private sealed class DelegateConfigurationProvider<TOptions> : IConfigurationProvider<TOptions>
        {
            private readonly Action<TOptions> _configure;

            public DelegateConfigurationProvider(Action<TOptions> configure)
            {
                _configure = configure;
            }

            public void Configure(TOptions configuration) => _configure(configuration);
        }
    }
}