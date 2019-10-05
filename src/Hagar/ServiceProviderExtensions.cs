using System;
using System.Buffers;
using System.Reflection;
using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.TypeSystem;
using Hagar.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hagar
{
    public interface IHagarBuilderImplementation : IHagarBuilder
    {
        IHagarBuilderImplementation ConfigureServices(Action<IServiceCollection> configureDelegate);
    }

    public interface IHagarBuilder 
    {
    }

    public class HagarBuilder : IHagarBuilderImplementation
    {
        private readonly IServiceCollection services;
        public HagarBuilder(IServiceCollection services) => this.services = services;

        public IHagarBuilderImplementation ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            configureDelegate(this.services);
            return this;
        }
    }

    public static class HagarBuilderExtensions
    {
        public static IHagarBuilder AddProvider(this IHagarBuilder builder, Func<IServiceProvider, IConfigurationProvider<SerializerConfiguration>> factory)
        {
            return ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.AddTransient(sp => factory(sp)));
        }

        public static IHagarBuilder AddProvider(this IHagarBuilder builder, IConfigurationProvider<SerializerConfiguration> provider)
        {
            return ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.AddSingleton(provider));
        }

        public static IHagarBuilder Configure(this IHagarBuilder builder, Action<SerializerConfiguration> configure)
        {
            return builder.AddProvider(new DelegateConfigurationProvider<SerializerConfiguration>(configure));
        }

        public static IHagarBuilder AddSerializers(this IHagarBuilder builder, Assembly assembly)
        {
            var attrs = assembly.GetCustomAttributes<MetadataProviderAttribute>();
            foreach (var attr in attrs)
            {
                if (!typeof(IConfigurationProvider<SerializerConfiguration>).IsAssignableFrom(attr.ProviderType)) continue;
                builder.AddProvider(sp => (IConfigurationProvider<SerializerConfiguration>)ActivatorUtilities.GetServiceOrCreateInstance(sp, attr.ProviderType));
            }

            return builder;
        }

        private sealed class DelegateConfigurationProvider<TOptions> : IConfigurationProvider<TOptions>
        {
            private readonly Action<TOptions> configure;

            public DelegateConfigurationProvider(Action<TOptions> configure)
            {
                this.configure = configure;
            }

            public void Configure(TOptions configuration) => this.configure(configuration);
        }
    }

    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddHagar(this IServiceCollection services, Action<IHagarBuilder> configure = null)
        {
            // Only add the services once.
            var context = GetFromServices<HagarConfigurationContext>(services);
            if (context is null)
            {
                context = new HagarConfigurationContext(services);
                services.Add(context.CreateServiceDescriptor());

                services.AddSingleton<IConfigurationProvider<SerializerConfiguration>, DefaultSerializerConfiguration>();
                services.AddSingleton<IConfigurationProvider<TypeConfiguration>, DefaultTypeConfiguration>();
                services.TryAddSingleton(typeof(IActivator<>), typeof(DefaultActivator<>));
                services.TryAddSingleton(typeof(ListActivator<>));
                services.TryAddSingleton(typeof(DictionaryActivator<,>));
                services.TryAddSingleton(typeof(IConfiguration<>), typeof(ConfigurationHolder<>));
                services.TryAddSingleton<ITypeResolver, CachedTypeResolver>();
                services.TryAddSingleton<CodecProvider>();
                services.TryAddSingleton<IUntypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<ITypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IPartialSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IValueSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddScoped(typeof(IFieldCodec<>), typeof(FieldCodecHolder<>));
                services.TryAddScoped(typeof(IPartialSerializer<>), typeof(PartialSerializerHolder<>));
                services.TryAddScoped(typeof(IValueSerializer<>), typeof(ValueSerializerHolder<>));
                services.TryAddSingleton<WellKnownTypeCollection>();
                services.TryAddSingleton<TypeCodec>();

                // Session
                services.AddSingleton<SessionPool>();

                // Serializer
                services.AddSingleton(typeof(Serializer<>));
            }

            configure?.Invoke(context.Builder);

            return services;
        }

        private static T GetFromServices<T>(IServiceCollection services)
        {
            foreach (var service in services )
            {
                if (service.ServiceType == typeof(T)) return (T)service.ImplementationInstance;
            }

            return default;
        }

        private sealed class HagarConfigurationContext
        {
            public HagarConfigurationContext(IServiceCollection services) => this.Builder = new HagarBuilder(services);

            public ServiceDescriptor CreateServiceDescriptor() => new ServiceDescriptor(typeof(HagarConfigurationContext), this);

            public HagarBuilder Builder { get; }
        }

        private sealed class FieldCodecHolder<TField> : IFieldCodec<TField>, IServiceHolder<IFieldCodec<TField>>
        {
            private readonly ITypedCodecProvider codecProvider;
            private IFieldCodec<TField> codec;

            public FieldCodecHolder(ITypedCodecProvider codecProvider)
            {
                this.codecProvider = codecProvider;
            }
            
            public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TField value) where TBufferWriter : IBufferWriter<byte> => this.Value.WriteField(ref writer, fieldIdDelta, expectedType, value);
            
            public TField ReadValue(ref Reader reader, Field field) => this.Value.ReadValue(ref reader, field);

            public IFieldCodec<TField> Value => this.codec ?? (this.codec = this.codecProvider.GetCodec<TField>());
        }

        private sealed class PartialSerializerHolder<TField> : IPartialSerializer<TField>, IServiceHolder<IPartialSerializer<TField>> where TField : class
        {
            private readonly IPartialSerializerProvider provider;
            private IPartialSerializer<TField> partialSerializer;

            public PartialSerializerHolder(IPartialSerializerProvider provider)
            {
                this.provider = provider;
            }

            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, TField value) where TBufferWriter : IBufferWriter<byte>
            {
                this.Value.Serialize(ref writer, value);
            }

            public void Deserialize(ref Reader reader, TField value)
            {
                this.Value.Deserialize(ref reader, value);
            }

            public IPartialSerializer<TField> Value => this.partialSerializer ?? (this.partialSerializer = this.provider.GetPartialSerializer<TField>());
        }

        private sealed class ValueSerializerHolder<TField> : IValueSerializer<TField>, IServiceHolder<IValueSerializer<TField>> where TField : struct
        {
            private readonly IValueSerializerProvider provider;
            private IValueSerializer<TField> serializer;

            public ValueSerializerHolder(IValueSerializerProvider provider)
            {
                this.provider = provider;
            }

            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, in TField value) where TBufferWriter : IBufferWriter<byte>
            {
                this.Value.Serialize(ref writer, in value);
            }

            public void Deserialize(ref Reader reader, ref TField value)
            {
                this.Value.Deserialize(ref reader, ref value);
            }

            public IValueSerializer<TField> Value => this.serializer ?? (this.serializer = this.provider.GetValueSerializer<TField>());
        }
    }
}
