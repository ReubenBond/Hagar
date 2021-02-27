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
using System;
using System.Buffers;

namespace Hagar
{
    public static class ServiceProviderExtensions
    {
        public static IServiceCollection AddHagar(this IServiceCollection services, Action<IHagarBuilder> configure = null)
        {
            // Only add the services once.
            var context = GetFromServices<HagarConfigurationContext>(services);
            if (context is null)
            {
                context = new HagarConfigurationContext(services);
                context.Builder.AddAssembly(typeof(ServiceProviderExtensions).Assembly);
                services.Add(context.CreateServiceDescriptor());
                services.AddSingleton<IConfigurationProvider<SerializerConfiguration>, DefaultTypeConfiguration>();
                services.AddSingleton<TypeResolver, CachedTypeResolver>();
                services.AddSingleton<TypeConverter>();
                services.TryAddSingleton(typeof(ListActivator<>));
                services.TryAddSingleton(typeof(DictionaryActivator<,>));
                services.TryAddSingleton(typeof(IConfiguration<>), typeof(ConfigurationHolder<>));
                services.TryAddSingleton<CodecProvider>();
                services.TryAddSingleton<ICodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IUntypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<ITypedCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IPartialSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IValueSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IActivatorProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddScoped(typeof(IFieldCodec<>), typeof(FieldCodecHolder<>));
                services.TryAddScoped(typeof(IPartialSerializer<>), typeof(PartialSerializerHolder<>));
                services.TryAddScoped(typeof(IValueSerializer<>), typeof(ValueSerializerHolder<>));
                services.TryAddSingleton(typeof(DefaultActivator<>));
                services.TryAddSingleton(typeof(IActivator<>), typeof(ActivatorHolder<>));
                services.TryAddSingleton<WellKnownTypeCollection>();
                services.TryAddSingleton<TypeCodec>();

                // Session
                services.TryAddSingleton<SerializerSessionPool>();

                // Serializer
                services.TryAddSingleton(typeof(Serializer));
                services.TryAddSingleton(typeof(Serializer<>));
                services.TryAddSingleton(typeof(ValueSerializer<>));
            }

            configure?.Invoke(context.Builder);

            return services;
        }

        private static T GetFromServices<T>(IServiceCollection services)
        {
            foreach (var service in services)
            {
                if (service.ServiceType == typeof(T))
                {
                    return (T)service.ImplementationInstance;
                }
            }

            return default;
        }

        private sealed class HagarConfigurationContext
        {
            public HagarConfigurationContext(IServiceCollection services) => Builder = new HagarBuilder(services);

            public ServiceDescriptor CreateServiceDescriptor() => new ServiceDescriptor(typeof(HagarConfigurationContext), this);

            public IHagarBuilder Builder { get; }
        }

        private class HagarBuilder : IHagarBuilderImplementation
        {
            private readonly IServiceCollection _services;

            public HagarBuilder(IServiceCollection services) => _services = services;

            public IHagarBuilderImplementation ConfigureServices(Action<IServiceCollection> configureDelegate)
            {
                configureDelegate(_services);
                return this;
            }
        }

        private sealed class ActivatorHolder<T> : IActivator<T>, IServiceHolder<IActivator<T>>
        {
            private readonly IActivatorProvider _activatorProvider;
            private IActivator<T> _activator;

            public ActivatorHolder(IActivatorProvider codecProvider)
            {
                _activatorProvider = codecProvider;
            }

            public IActivator<T> Value => _activator ??= _activatorProvider.GetActivator<T>();

            public T Create() => Value.Create();
        }

        private sealed class FieldCodecHolder<TField> : IFieldCodec<TField>, IServiceHolder<IFieldCodec<TField>>
        {
            private readonly ITypedCodecProvider _codecProvider;
            private IFieldCodec<TField> _codec;

            public FieldCodecHolder(ITypedCodecProvider codecProvider)
            {
                _codecProvider = codecProvider;
            }

            public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TField value) where TBufferWriter : IBufferWriter<byte> => Value.WriteField(ref writer, fieldIdDelta, expectedType, value);

            public TField ReadValue<TInput>(ref Reader<TInput> reader, Field field) => Value.ReadValue(ref reader, field);

            public IFieldCodec<TField> Value => _codec ??= _codecProvider.GetCodec<TField>();
        }

        private sealed class PartialSerializerHolder<TField> : IPartialSerializer<TField>, IServiceHolder<IPartialSerializer<TField>> where TField : class
        {
            private readonly IPartialSerializerProvider _provider;
            private IPartialSerializer<TField> _partialSerializer;

            public PartialSerializerHolder(IPartialSerializerProvider provider)
            {
                _provider = provider;
            }

            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, TField value) where TBufferWriter : IBufferWriter<byte> => Value.Serialize(ref writer, value);

            public void Deserialize<TInput>(ref Reader<TInput> reader, TField value) => Value.Deserialize(ref reader, value);

            public IPartialSerializer<TField> Value => _partialSerializer ??= _provider.GetPartialSerializer<TField>();
        }

        private sealed class ValueSerializerHolder<TField> : IValueSerializer<TField>, IServiceHolder<IValueSerializer<TField>> where TField : struct
        {
            private readonly IValueSerializerProvider _provider;
            private IValueSerializer<TField> _serializer;

            public ValueSerializerHolder(IValueSerializerProvider provider)
            {
                _provider = provider;
            }

            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, ref TField value) where TBufferWriter : IBufferWriter<byte> => Value.Serialize(ref writer, ref value);

            public void Deserialize<TInput>(ref Reader<TInput> reader, ref TField value) => Value.Deserialize(ref reader, ref value);

            public IValueSerializer<TField> Value => _serializer ??= _provider.GetValueSerializer<TField>();
        }
    }
}