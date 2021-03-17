using Hagar.Activators;
using Hagar.Cloning;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.GeneratedCodeHelpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Hagar.Serializers
{
    public sealed class CodecProvider : ICodecProvider
    {
        private static readonly Type ObjectType = typeof(object);
        private static readonly Type OpenGenericCodecType = typeof(IFieldCodec<>);
        private static readonly MethodInfo TypedCodecWrapperCreateMethod = typeof(CodecAdapter).GetMethod(nameof(CodecAdapter.CreateUntypedFromTyped), BindingFlags.Public | BindingFlags.Static);

        private static readonly Type OpenGenericCopierType = typeof(IDeepCopier<>);
        private static readonly MethodInfo TypedCopierWrapperCreateMethod = typeof(CopierAdapter).GetMethod(nameof(CopierAdapter.CreateUntypedFromTyped), BindingFlags.Public | BindingFlags.Static);

        private readonly object _initializationLock = new object();

        private readonly ConcurrentDictionary<(Type, Type), IFieldCodec> _adaptedCodecs = new ConcurrentDictionary<(Type, Type), IFieldCodec>();
        private readonly ConcurrentDictionary<(Type, Type), IDeepCopier> _adaptedCopiers = new ConcurrentDictionary<(Type, Type), IDeepCopier>();

        private readonly ConcurrentDictionary<Type, object> _instantiatedPartialSerializers = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, object> _instantiatedPartialCopiers = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, object> _instantiatedValueSerializers = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, object> _instantiatedActivators = new ConcurrentDictionary<Type, object>();
        private readonly Dictionary<Type, Type> _partialSerializers = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _valueSerializers = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _fieldCodecs = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _copiers = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _partialCopiers = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> _activators = new Dictionary<Type, Type>();
        private readonly List<IGeneralizedCodec> _generalizedCodecs = new List<IGeneralizedCodec>();
        private readonly List<IGeneralizedCopier> _generalizedCopiers = new List<IGeneralizedCopier>();
        private readonly VoidCodec _voidCodec = new VoidCodec();
        private readonly ObjectCopier _objectCopier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDeepCopier _voidCopier = new VoidCopier();
        private bool _initialized;

        public CodecProvider(IServiceProvider serviceProvider, IConfiguration<SerializerConfiguration> codecConfiguration)
        {
            _serviceProvider = serviceProvider;

            // Hard-code the object codec because many codecs implement IFieldCodec<object> and this is cleaner
            // than adding some filtering logic to find "the object codec" below.
            _fieldCodecs[typeof(object)] = typeof(ObjectCodec);
            _copiers[typeof(object)] = typeof(ObjectCopier);
            _objectCopier = new ObjectCopier();

            ConsumeMetadata(codecConfiguration);
        }

        public IServiceProvider Services => _serviceProvider;

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
                _generalizedCodecs.AddRange(_serviceProvider.GetServices<IGeneralizedCodec>());
                _generalizedCopiers.AddRange(_serviceProvider.GetServices<IGeneralizedCopier>());
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
            AddFromMetadata(_copiers, metadata.Copiers, typeof(IDeepCopier<>));
            AddFromMetadata(_partialCopiers, metadata.Copiers, typeof(IPartialCopier<>));

            static void AddFromMetadata(IDictionary<Type, Type> resultCollection, IEnumerable<Type> metadataCollection, Type genericType)
            {
                Debug.Assert(genericType.GetGenericArguments().Length == 1);

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

        public IFieldCodec<TField> TryGetCodec<TField>() => TryGetCodecInner<TField>(typeof(TField));

        public IFieldCodec<object> GetCodec(Type fieldType) => TryGetCodec(fieldType) ?? ThrowCodecNotFound<object>(fieldType);

        public IFieldCodec<object> TryGetCodec(Type fieldType) => TryGetCodecInner<object>(fieldType);

        public IFieldCodec<TField> GetCodec<TField>() => TryGetCodec<TField>() ?? ThrowCodecNotFound<TField>(typeof(TField));

        private IFieldCodec<TField> TryGetCodecInner<TField>(Type fieldType)
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
                        foreach (var dynamicCodec in _generalizedCodecs)
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
                case IWrappedCodec wrapped when wrapped.Inner is IFieldCodec<TField> typedCodec:
                    typedResult = typedCodec;
                    wasAdapted = true;
                    break;
                case IFieldCodec<object> objectCodec:
                    typedResult = CodecAdapter.CreateTypedFromUntyped<TField>(objectCodec);
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

        public IPartialCopier<TField> GetPartialCopier<TField>() where TField : class
        {
            if (!_initialized)
            {
                Initialize();
            }

            ThrowIfUnsupportedType(typeof(TField));
            var type = typeof(TField);
            var searchType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            return GetPartialCopierInner<TField>(type, searchType) ?? ThrowPartialCopierNotFound<TField>(type);
        }

        public IDeepCopier<T> GetDeepCopier<T>() => TryGetCopierInner<T>(typeof(T)) ?? ThrowCopierNotFound<T>(typeof(T));

        public IDeepCopier<T> TryGetDeepCopier<T>() => TryGetCopierInner<T>(typeof(T));

        public IDeepCopier<object> GetDeepCopier(Type fieldType) => TryGetCopierInner<object>(fieldType) ?? ThrowCopierNotFound<object>(fieldType);

        public IDeepCopier<object> TryGetDeepCopier(Type fieldType) => TryGetCopierInner<object>(fieldType);
        
        private IDeepCopier<T> TryGetCopierInner<T>(Type fieldType)
        {
            if (!_initialized)
            {
                Initialize();
            }

            var resultFieldType = typeof(T);
            var wasCreated = false;

            // Try to find the copier from the configured copiers.
            IDeepCopier untypedResult;

            // If the field type is unavailable, return the void copier which can at least handle references.
            if (fieldType is null)
            {
                untypedResult = _voidCopier;
            }
            else if (!_adaptedCopiers.TryGetValue((fieldType, resultFieldType), out untypedResult))
            {
                ThrowIfUnsupportedType(fieldType);

                if (fieldType.IsConstructedGenericType)
                {
                    untypedResult = CreateCopierInstance(fieldType, fieldType.GetGenericTypeDefinition());
                }
                else
                {
                    untypedResult = CreateCopierInstance(fieldType, fieldType);
                    if (untypedResult is null)
                    {
                        foreach (var dynamicCopier in _generalizedCopiers)
                        {
                            if (dynamicCopier.IsSupportedType(fieldType))
                            {
                                untypedResult = dynamicCopier;
                                break;
                            }
                        }
                    }
                }

                if (untypedResult is null && (fieldType.IsInterface || fieldType.IsAbstract))
                {
                    untypedResult = _objectCopier;
                }

                wasCreated = untypedResult != null;
            }

            // Attempt to adapt the copier if it's not already adapted.
            IDeepCopier<T> typedResult;
            var wasAdapted = false;
            switch (untypedResult)
            {
                case null:
                    return null;
                case IDeepCopier<T> typedCopier:
                    typedResult = typedCopier;
                    break;
                case IWrappedCodec wrapped when wrapped.Inner is IDeepCopier<T> typedCopier:
                    typedResult = typedCopier;
                    wasAdapted = true;
                    break;
                case IDeepCopier<object> objectCopier:
                    typedResult = CopierAdapter.CreateTypedFromUntyped<T>(objectCopier);
                    wasAdapted = true;
                    break;
                default:
                    typedResult = TryWrapCopier(untypedResult);
                    wasAdapted = true;
                    break;
            }

            // Store the results or throw if adaptation failed.
            if (typedResult is not null && (wasCreated || wasAdapted))
            {
                untypedResult = typedResult;
                var key = (fieldType, resultFieldType);
                if (_adaptedCopiers.TryGetValue(key, out var existing))
                {
                    typedResult = (IDeepCopier<T>)existing;
                }
                else if (!_adaptedCopiers.TryAdd(key, untypedResult))
                {
                    typedResult = (IDeepCopier<T>)_adaptedCopiers[key];
                }
            }
            else if (typedResult is null)
            {
                ThrowCannotConvert(untypedResult);
            }

            return typedResult;

            static IDeepCopier<T> TryWrapCopier(object rawCopier)
            {
                var copierType = rawCopier.GetType();
                if (typeof(T) == ObjectType)
                {
                    foreach (var @interface in copierType.GetInterfaces())
                    {
                        if (@interface.IsConstructedGenericType
                            && OpenGenericCopierType.IsAssignableFrom(@interface.GetGenericTypeDefinition()))
                        {
                            // Convert the typed copier provider into a wrapped object copier provider.
                            return TypedCopierWrapperCreateMethod.MakeGenericMethod(@interface.GetGenericArguments()[0], copierType).Invoke(null, new[] { rawCopier }) as IDeepCopier<T>;
                        }
                    }
                }

                return null;
            }

            static void ThrowCannotConvert(object rawCopier)
            {
                throw new InvalidOperationException($"Cannot convert copier of type {rawCopier.GetType()} to copier of type {typeof(IDeepCopier<T>)}.");
            }
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

        private IPartialCopier<T> GetPartialCopierInner<T>(Type concreteType, Type searchType) where T : class
        {
            if (!_partialCopiers.TryGetValue(searchType, out var copierType))
            {
                return null;
            }

            if (copierType.IsGenericTypeDefinition)
            {
                copierType = copierType.MakeGenericType(concreteType.GetGenericArguments());
            }

            if (!_instantiatedPartialCopiers.TryGetValue(copierType, out var result))
            {
                result = GetServiceOrCreateInstance(copierType);
                _ = _instantiatedPartialCopiers.TryAdd(copierType, result);
            }

            return (IPartialCopier<T>)result;
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
            else if (fieldType.IsEnum)
            {
                return CreateCodecInstance(fieldType, fieldType.GetEnumUnderlyingType());
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

        private IDeepCopier CreateCopierInstance(Type fieldType, Type searchType)
        {
            object[] constructorArguments = null;
            if (_copiers.TryGetValue(searchType, out var copierType))
            {
                if (copierType.IsGenericTypeDefinition)
                {
                    copierType = copierType.MakeGenericType(fieldType.GetGenericArguments());
                }
            }
            else if (ShallowCopyableTypes.Contains(fieldType))
            {
                copierType = typeof(ShallowCopyableTypeCopier<>).MakeGenericType(fieldType);
            }
            /*
            else if (_partialCopiers.TryGetValue(searchType, out var partialCopierType))
            {
                if (partialCopierType.IsGenericTypeDefinition)
                {
                    partialCopierType = partialCopierType.MakeGenericType(fieldType.GetGenericArguments());
                }

                // If there is a partial serializer for this type, create a copier which will then accept that partial serializer.
                copierType = typeof(ConcreteTypeCopier<,>).MakeGenericType(fieldType, partialCopierType);
                constructorArguments = new[] { GetServiceOrCreateInstance(partialCopierType) };
            }
            else if (_valueCopiers.TryGetValue(searchType, out var valueCopierType))
            {
                if (valueCopierType.IsGenericTypeDefinition)
                {
                    valueCopierType = valueCopierType.MakeGenericType(fieldType.GetGenericArguments());
                }

                // If there is a value serializer for this type, create a copier which will then accept that value serializer.
                copierType = typeof(ValueCopier<,>).MakeGenericType(fieldType, valueCopierType);
                constructorArguments = new[] { GetServiceOrCreateInstance(valueCopierType) };
            }
            */
            else if (fieldType.IsArray)
            {
                // Depending on the rank of the array (1 or higher), select the base array copier or the multi-dimensional copier.
                var arrayCopierType = fieldType.GetArrayRank() == 1 ? typeof(ArrayCopier<>) : typeof(MultiDimensionalArrayCopier<>);
                copierType = arrayCopierType.MakeGenericType(fieldType.GetElementType());
            }
            else if (searchType.BaseType is object && CreateCopierInstance(fieldType, searchType.BaseType) is IDeepCopier baseCopier)
            {
                // Find copiers which generalize over all subtypes.
                if (baseCopier is IDerivedTypeCopier)
                {
                    return baseCopier;
                }
            }

            return copierType != null ? (IDeepCopier)GetServiceOrCreateInstance(copierType, constructorArguments) : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowPointerType(Type fieldType) => throw new NotSupportedException($"Type {fieldType} is a pointer type and is therefore not supported.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowByRefType(Type fieldType) => throw new NotSupportedException($"Type {fieldType} is a by-ref type and is therefore not supported.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowGenericTypeDefinition(Type fieldType) => throw new InvalidOperationException($"Type {fieldType} is a non-constructed generic type and is therefore unsupported.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IFieldCodec<TField> ThrowCodecNotFound<TField>(Type fieldType) => throw new CodecNotFoundException($"Could not find a codec for type {fieldType}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IDeepCopier<T> ThrowCopierNotFound<T>(Type type) => throw new CodecNotFoundException($"Could not find a copier for type {type}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IPartialSerializer<TField> ThrowPartialSerializerNotFound<TField>(Type fieldType) where TField : class => throw new KeyNotFoundException($"Could not find a partial serializer for type {fieldType}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IValueSerializer<TField> ThrowValueSerializerNotFound<TField>(Type fieldType) where TField : struct => throw new KeyNotFoundException($"Could not find a value serializer for type {fieldType}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IActivator<T> ThrowActivatorNotFound<T>(Type type) => throw new KeyNotFoundException($"Could not find an activator for type {type}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IPartialCopier<T> ThrowPartialCopierNotFound<T>(Type type) where T : class => throw new KeyNotFoundException($"Could not find a partial copier for type {type}.");
    }
}