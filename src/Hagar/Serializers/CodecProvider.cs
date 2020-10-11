using Hagar.Activators;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.GeneratedCodeHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace Hagar.Serializers
{
    public sealed class CodecProvider : ICodecProvider
    {
        private static readonly Type ObjectType = typeof(object);
        private static readonly Type OpenGenericCodecType = typeof(IFieldCodec<>);
        private static readonly MethodInfo TypedCodecWrapperCreateMethod = typeof(CodecAdapter).GetMethod(nameof(CodecAdapter.CreateUntypedFromTyped), BindingFlags.Public | BindingFlags.Static);

        private readonly object _initializationLock = new object();
        private readonly ConcurrentDictionary<(Type, Type), IFieldCodec> _adaptedCodecs = new ConcurrentDictionary<(Type, Type), IFieldCodec>();
        private readonly ConcurrentDictionary<Type, object> _instantiatedPartialSerializers = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, object> _instantiatedValueSerializers = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, object> _instantiatedActivators = new ConcurrentDictionary<Type, object>();
        private readonly Dictionary<Type, Type> _partialSerializers = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _valueSerializers = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _fieldCodecs = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _activators = new Dictionary<Type, Type>();
        private readonly List<IGeneralizedCodec> _generalized = new List<IGeneralizedCodec>();
        private readonly VoidCodec _voidCodec = new VoidCodec();
        private readonly IServiceProvider _serviceProvider;
        private bool _initialized;

        public CodecProvider(IServiceProvider serviceProvider, IConfiguration<SerializerConfiguration> codecConfiguration)
        {
            _serviceProvider = serviceProvider;

            // Hard-code the object codec because many codecs implement IFieldCodec<object> and this is cleaner
            // than adding some filtering logic to find "the object codec" below.
            _fieldCodecs[typeof(object)] = typeof(ObjectCodec);

            ConsumeMetadata(codecConfiguration);
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            lock (_initializationLock)
            {
                if (_initialized)
                {
                    return;
                }

                _initialized = true;
                _generalized.AddRange(_serviceProvider.GetServices<IGeneralizedCodec>());
            }
        }

        private void ConsumeMetadata(IConfiguration<SerializerConfiguration> codecConfiguration)
        {
            var metadata = codecConfiguration.Value;
            AddFromMetadata(_partialSerializers, metadata.Serializers, typeof(IPartialSerializer<>));
            AddFromMetadata(_valueSerializers, metadata.Serializers, typeof(IValueSerializer<>));
            AddFromMetadata(_fieldCodecs, metadata.Serializers, typeof(IFieldCodec<>));
            AddFromMetadata(_fieldCodecs, metadata.FieldCodecs, typeof(IFieldCodec<>));
            AddFromMetadata(_activators, metadata.Activators, typeof(IActivator<>));

            static void AddFromMetadata(IDictionary<Type, Type> resultCollection, IEnumerable<Type> metadataCollection, Type genericType)
            {
                if (genericType.GetGenericArguments().Length != 1)
                {
                    throw new ArgumentException($"Type {genericType} must have an arity of 1.");
                }

                foreach (var type in metadataCollection)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        if (!@interface.IsGenericType)
                        {
                            continue;
                        }

                        if (genericType != @interface.GetGenericTypeDefinition())
                        {
                            continue;
                        }

                        var genericArgument = @interface.GetGenericArguments()[0];
                        if (typeof(object) == genericArgument)
                        {
                            continue;
                        }

                        if (genericArgument.IsConstructedGenericType && genericArgument.GenericTypeArguments.Any(arg => arg.IsGenericParameter))
                        {
                            genericArgument = genericArgument.GetGenericTypeDefinition();
                        }

                        resultCollection[genericArgument] = type;
                    }
                }
            }
        }

        public IFieldCodec<TField> TryGetCodec<TField>() => TryGetCodec<TField>(typeof(TField));

        public IFieldCodec<object> GetCodec(Type fieldType) => TryGetCodec(fieldType) ?? ThrowCodecNotFound<object>(fieldType);

        public IFieldCodec<object> TryGetCodec(Type fieldType) => TryGetCodec<object>(fieldType);

        public IFieldCodec<TField> GetCodec<TField>() => TryGetCodec<TField>() ?? ThrowCodecNotFound<TField>(typeof(TField));

        private IFieldCodec<TField> TryGetCodec<TField>(Type fieldType)
        {
            if (!_initialized)
            {
                Initialize();
            }

            var resultFieldType = typeof(TField);
            var wasCreated = false;

            // Try to find the codec from the configured codecs.
            IFieldCodec untypedResult;

            // If the field type is unavailable, return the void codec which can at least handle references.
            if (fieldType is null)
            {
                untypedResult = _voidCodec;
            }
            else if (!_adaptedCodecs.TryGetValue((fieldType, resultFieldType), out untypedResult))
            {
                ThrowIfUnsupportedType(fieldType);

                if (fieldType.IsConstructedGenericType)
                {
                    untypedResult = CreateCodecInstance(fieldType, fieldType.GetGenericTypeDefinition());
                }
                else
                {
                    untypedResult = CreateCodecInstance(fieldType, fieldType);
                    if (untypedResult is null)
                    {
                        foreach (var dynamicCodec in _generalized)
                        {
                            if (dynamicCodec.IsSupportedType(fieldType))
                            {
                                untypedResult = dynamicCodec;
                                break;
                            }
                        }
                    }
                }

                if (untypedResult is null && (fieldType.IsInterface || fieldType.IsAbstract))
                {
                    untypedResult = (IFieldCodec)GetServiceOrCreateInstance(typeof(AbstractTypeSerializer<>).MakeGenericType(fieldType));
                }

                wasCreated = untypedResult != null;
            }

            // Attempt to adapt the codec if it's not already adapted.
            IFieldCodec<TField> typedResult;
            var wasAdapted = false;
            switch (untypedResult)
            {
                case null:
                    return null;
                case IFieldCodec<TField> typedCodec:
                    typedResult = typedCodec;
                    break;
                case IWrappedCodec wrapped when wrapped.InnerCodec is IFieldCodec<TField> typedCodec:
                    typedResult = typedCodec;
                    wasAdapted = true;
                    break;
                case IFieldCodec<object> objectCodec:
                    typedResult = CodecAdapter.CreatedTypedFromUntyped<TField>(objectCodec);
                    wasAdapted = true;
                    break;
                default:
                    typedResult = TryWrapCodec(untypedResult);
                    wasAdapted = true;
                    break;
            }

            // Store the results or throw if adaptation failed.
            if (typedResult != null && (wasCreated || wasAdapted))
            {
                untypedResult = typedResult;
                var key = (fieldType, resultFieldType);
                if (_adaptedCodecs.TryGetValue(key, out var existing))
                {
                    typedResult = (IFieldCodec<TField>)existing;
                }
                else if (!_adaptedCodecs.TryAdd(key, untypedResult))
                {
                    typedResult = (IFieldCodec<TField>)_adaptedCodecs[key];
                }
            }
            else if (typedResult is null)
            {
                ThrowCannotConvert(untypedResult);
            }

            return typedResult;

            static IFieldCodec<TField> TryWrapCodec(object rawCodec)
            {
                var codecType = rawCodec.GetType();
                if (typeof(TField) == ObjectType)
                {
                    foreach (var @interface in codecType.GetInterfaces())
                    {
                        if (@interface.IsConstructedGenericType
                            && OpenGenericCodecType.IsAssignableFrom(@interface.GetGenericTypeDefinition()))
                        {
                            // Convert the typed codec provider into a wrapped object codec provider.
                            return TypedCodecWrapperCreateMethod.MakeGenericMethod(@interface.GetGenericArguments()[0], codecType).Invoke(null, new[] { rawCodec }) as IFieldCodec<TField>;
                        }
                    }
                }

                return null;
            }

            static void ThrowCannotConvert(object rawCodec)
            {
                throw new InvalidOperationException($"Cannot convert codec of type {rawCodec.GetType()} to codec of type {typeof(IFieldCodec<TField>)}.");
            }
        }

        public IActivator<T> GetActivator<T>()
        {
            if (!_initialized)
            {
                Initialize();
            }

            ThrowIfUnsupportedType(typeof(T));
            var type = typeof(T);
            var searchType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            return GetActivatorInner<T>(type, searchType) ?? ThrowActivatorNotFound<T>(type);
        }

        public IPartialSerializer<TField> GetPartialSerializer<TField>() where TField : class
        {
            if (!_initialized)
            {
                Initialize();
            }

            ThrowIfUnsupportedType(typeof(TField));
            var type = typeof(TField);
            var searchType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            return GetPartialSerializerInner<TField>(type, searchType) ?? ThrowPartialSerializerNotFound<TField>(type);
        }

        public IValueSerializer<TField> GetValueSerializer<TField>() where TField : struct
        {
            if (!_initialized)
            {
                Initialize();
            }

            ThrowIfUnsupportedType(typeof(TField));
            var type = typeof(TField);
            var searchType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            return GetValueSerializerInner<TField>(type, searchType) ?? ThrowValueSerializerNotFound<TField>(type);
        }

        private IPartialSerializer<TField> GetPartialSerializerInner<TField>(Type concreteType, Type searchType) where TField : class
        {
            if (!_partialSerializers.TryGetValue(searchType, out var serializerType))
            {
                return null;
            }

            if (serializerType.IsGenericTypeDefinition)
            {
                serializerType = serializerType.MakeGenericType(concreteType.GetGenericArguments());
            }

            if (!_instantiatedPartialSerializers.TryGetValue(serializerType, out var result))
            {
                result = GetServiceOrCreateInstance(serializerType);
                _ = _instantiatedPartialSerializers.TryAdd(serializerType, result);
            }

            return (IPartialSerializer<TField>)result;
        }

        private IValueSerializer<TField> GetValueSerializerInner<TField>(Type concreteType, Type searchType) where TField : struct
        {
            if (!_valueSerializers.TryGetValue(searchType, out var serializerType))
            {
                return null;
            }

            if (serializerType.IsGenericTypeDefinition)
            {
                serializerType = serializerType.MakeGenericType(concreteType.GetGenericArguments());
            }

            if (!_instantiatedValueSerializers.TryGetValue(serializerType, out var result))
            {
                result = GetServiceOrCreateInstance(serializerType);
                _ = _instantiatedValueSerializers.TryAdd(serializerType, result);
            }

            return (IValueSerializer<TField>)result;
        }

        private IActivator<T> GetActivatorInner<T>(Type concreteType, Type searchType)
        {
            if (!_activators.TryGetValue(searchType, out var activatorType))
            {
                activatorType = typeof(DefaultActivator<>).MakeGenericType(concreteType);
            }

            if (activatorType.IsGenericTypeDefinition)
            {
                activatorType = activatorType.MakeGenericType(concreteType.GetGenericArguments());
            }

            if (!_instantiatedActivators.TryGetValue(activatorType, out var result))
            {
                result = GetServiceOrCreateInstance(activatorType);
                _ = _instantiatedActivators.TryAdd(activatorType, result);
            }

            return (IActivator<T>)result;
        }

        private static void ThrowIfUnsupportedType(Type fieldType)
        {
            if (fieldType.IsGenericTypeDefinition)
            {
                ThrowGenericTypeDefinition(fieldType);
            }

            if (fieldType.IsPointer)
            {
                ThrowPointerType(fieldType);
            }

            if (fieldType.IsByRef)
            {
                ThrowByRefType(fieldType);
            }
        }

        private object GetServiceOrCreateInstance(Type type, object[] constructorArguments = null)
        {
            var result = HagarGeneratedCodeHelper.TryGetService(type);
            if (result != null)
            {
                return result;
            }

            result = _serviceProvider.GetService(type);
            if (result != null)
            {
                return result;
            }

            result = ActivatorUtilities.CreateInstance(_serviceProvider, type, constructorArguments ?? Array.Empty<object>());
            return result;
        }

        private IFieldCodec CreateCodecInstance(Type fieldType, Type searchType)
        {
            object[] constructorArguments = null;
            if (_fieldCodecs.TryGetValue(searchType, out var codecType))
            {
                if (codecType.IsGenericTypeDefinition)
                {
                    codecType = codecType.MakeGenericType(fieldType.GetGenericArguments());
                }
            }
            else if (_partialSerializers.TryGetValue(searchType, out var partialSerializerType))
            {
                if (partialSerializerType.IsGenericTypeDefinition)
                {
                    partialSerializerType = partialSerializerType.MakeGenericType(fieldType.GetGenericArguments());
                }

                // If there is a partial serializer for this type, create a codec which will then accept that partial serializer.
                codecType = typeof(ConcreteTypeSerializer<,>).MakeGenericType(fieldType, partialSerializerType);
                constructorArguments = new[] { GetServiceOrCreateInstance(partialSerializerType) };
            }
            else if (_valueSerializers.TryGetValue(searchType, out var valueSerializerType))
            {
                if (valueSerializerType.IsGenericTypeDefinition)
                {
                    valueSerializerType = valueSerializerType.MakeGenericType(fieldType.GetGenericArguments());
                }

                // If there is a value serializer for this type, create a codec which will then accept that value serializer.
                codecType = typeof(ValueSerializer<,>).MakeGenericType(fieldType, valueSerializerType);
                constructorArguments = new[] { GetServiceOrCreateInstance(valueSerializerType) };
            }
            else if (fieldType.IsArray)
            {
                // Depending on the rank of the array (1 or higher), select the base array codec or the multi-dimensional codec.
                var arrayCodecType = fieldType.GetArrayRank() == 1 ? typeof(ArrayCodec<>) : typeof(MultiDimensionalArrayCodec<>);
                codecType = arrayCodecType.MakeGenericType(fieldType.GetElementType());
            }
            else if (searchType.BaseType is object && CreateCodecInstance(fieldType, searchType.BaseType) is IFieldCodec baseCodec)
            {
                // Find codecs which generalize over all subtypes.
                if (baseCodec is IDerivedTypeCodec)
                {
                    return baseCodec;
                }
            }

            return codecType != null ? (IFieldCodec)GetServiceOrCreateInstance(codecType, constructorArguments) : null;
        }

        private static void ThrowPointerType(Type fieldType) => throw new NotSupportedException($"Type {fieldType} is a pointer type and is therefore not supported.");

        private static void ThrowByRefType(Type fieldType) => throw new NotSupportedException($"Type {fieldType} is a by-ref type and is therefore not supported.");

        private static void ThrowGenericTypeDefinition(Type fieldType) => throw new InvalidOperationException($"Type {fieldType} is a non-constructed generic type and is therefore unsupported.");

        private static IFieldCodec<TField> ThrowCodecNotFound<TField>(Type fieldType) => throw new CodecNotFoundException($"Could not find a codec for type {fieldType}.");

        private static IPartialSerializer<TField> ThrowPartialSerializerNotFound<TField>(Type fieldType) where TField : class => throw new KeyNotFoundException($"Could not find a partial serializer for type {fieldType}.");

        private static IValueSerializer<TField> ThrowValueSerializerNotFound<TField>(Type fieldType) where TField : struct => throw new KeyNotFoundException($"Could not find a value serializer for type {fieldType}.");

        private static IActivator<T> ThrowActivatorNotFound<T>(Type type) => throw new KeyNotFoundException($"Could not find an activator for type {type}.");
    }
}