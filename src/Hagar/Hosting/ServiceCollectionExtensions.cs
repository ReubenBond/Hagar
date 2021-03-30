using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Cloning;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.ISerializableSupport;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.TypeSystem;
using Hagar.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Hagar
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHagar(this IServiceCollection services, Action<IHagarBuilder> configure = null)
        {
            // Only add the services once.
            var context = GetFromServices<HagarConfigurationContext>(services);
            if (context is null)
            {
                context = new HagarConfigurationContext(services);
                foreach (var asm in ReferencedAssemblyHelper.GetRelevantAssemblies(services))
                {
                    context.Builder.AddAssembly(asm);
                }

                services.Add(context.CreateServiceDescriptor());
                services.AddSingleton<IConfigurationProvider<SerializerConfiguration>, DefaultTypeConfiguration>();
                services.AddSingleton<TypeResolver, CachedTypeResolver>();
                services.AddSingleton<TypeConverter>();
                services.TryAddSingleton(typeof(ListActivator<>));
                services.TryAddSingleton(typeof(DictionaryActivator<,>));
                services.TryAddSingleton(typeof(IConfiguration<>), typeof(ConfigurationHolder<>));
                services.TryAddSingleton<CodecProvider>();
                services.TryAddSingleton<ICodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IDeepCopierProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IFieldCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IBaseCodecProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IValueSerializerProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddSingleton<IActivatorProvider>(sp => sp.GetRequiredService<CodecProvider>());
                services.TryAddScoped(typeof(IFieldCodec<>), typeof(FieldCodecHolder<>));
                services.TryAddScoped(typeof(IBaseCodec<>), typeof(BaseCodecHolder<>));
                services.TryAddScoped(typeof(IValueSerializer<>), typeof(ValueSerializerHolder<>));
                services.TryAddSingleton(typeof(DefaultActivator<>));
                services.TryAddSingleton(typeof(IActivator<>), typeof(ActivatorHolder<>));
                services.TryAddSingleton<WellKnownTypeCollection>();
                services.TryAddSingleton<TypeCodec>();
                services.TryAddScoped(typeof(IDeepCopier<>), typeof(CopierHolder<>));
                services.TryAddScoped(typeof(IBaseCopier<>), typeof(BaseCopierHolder<>));

                // Type filtering
                services.AddSingleton<ITypeFilter, DefaultTypeFilter>();

                // Session
                services.TryAddSingleton<SerializerSessionPool>();
                services.TryAddSingleton<CopyContextPool>();

                services.AddSingleton<IGeneralizedCodec, CompareInfoCodec>();
                services.AddSingleton<IGeneralizedCopier, CompareInfoCopier>();
                services.AddSingleton<IGeneralizedCodec, WellKnownStringComparerCodec>();

                services.AddSingleton<ExceptionCodec>();
                services.AddSingleton<IGeneralizedCodec>(sp => sp.GetRequiredService<ExceptionCodec>());

                // Serializer
                services.TryAddSingleton<Serializer>();
                services.TryAddSingleton(typeof(Serializer<>));
                services.TryAddSingleton(typeof(ValueSerializer<>));
                services.TryAddSingleton<DeepCopier>();
                services.TryAddSingleton(typeof(DeepCopier<>));
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

            public Dictionary<object, object> Properties { get; } = new();

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
            private readonly IFieldCodecProvider _codecProvider;
            private IFieldCodec<TField> _codec;

            public FieldCodecHolder(IFieldCodecProvider codecProvider)
            {
                _codecProvider = codecProvider;
            }

            public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TField value) where TBufferWriter : IBufferWriter<byte> => Value.WriteField(ref writer, fieldIdDelta, expectedType, value);

            public TField ReadValue<TInput>(ref Reader<TInput> reader, Field field) => Value.ReadValue(ref reader, field);

            public IFieldCodec<TField> Value => _codec ??= _codecProvider.GetCodec<TField>();
        }

        private sealed class BaseCodecHolder<TField> : IBaseCodec<TField>, IServiceHolder<IBaseCodec<TField>> where TField : class
        {
            private readonly IBaseCodecProvider _provider;
            private IBaseCodec<TField> _baseCodec;

            public BaseCodecHolder(IBaseCodecProvider provider)
            {
                _provider = provider;
            }

            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, TField value) where TBufferWriter : IBufferWriter<byte> => Value.Serialize(ref writer, value);

            public void Deserialize<TInput>(ref Reader<TInput> reader, TField value) => Value.Deserialize(ref reader, value);

            public IBaseCodec<TField> Value => _baseCodec ??= _provider.GetBaseCodec<TField>();
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

        private sealed class CopierHolder<T> : IDeepCopier<T>, IServiceHolder<IDeepCopier<T>>
        {
            private readonly IDeepCopierProvider _codecProvider;
            private IDeepCopier<T> _copier;

            public CopierHolder(IDeepCopierProvider codecProvider)
            {
                _codecProvider = codecProvider;
            }

            public T DeepCopy(T original, CopyContext context) => Value.DeepCopy(original, context);

            public IDeepCopier<T> Value => _copier ??= _codecProvider.GetDeepCopier<T>();
        }

        private sealed class BaseCopierHolder<T> : IBaseCopier<T>, IServiceHolder<IBaseCopier<T>> where T : class
        {
            private readonly IDeepCopierProvider _codecProvider;
            private IBaseCopier<T> _copier;

            public BaseCopierHolder(IDeepCopierProvider codecProvider)
            {
                _codecProvider = codecProvider;
            }

            public void DeepCopy(T original, T copy, CopyContext context) => Value.DeepCopy(original, copy, context);

            public IBaseCopier<T> Value => _copier ??= _codecProvider.GetBaseCopier<T>();
        }
    }
}
