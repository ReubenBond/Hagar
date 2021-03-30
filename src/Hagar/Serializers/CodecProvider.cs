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

        private readonly object _initializationLock = new();

        private readonly ConcurrentDictionary<(Type, Type), IFieldCodec> _adaptedCodecs = new();
        private readonly ConcurrentDictionary<(Type, Type), IDeepCopier> _adaptedCopiers = new();

        private readonly ConcurrentDictionary<Type, object> _instantiatedBaseCodecs = new();
        private readonly ConcurrentDictionary<Type, object> _instantiatedBaseCopiers = new();
        private readonly ConcurrentDictionary<Type, object> _instantiatedValueSerializers = new();
        private readonly ConcurrentDictionary<Type, object> _instantiatedActivators = new();
        private readonly Dictionary<Type, Type> _baseCodecs = new();
        private readonly Dictionary<Type, Type> _valueSerializers = new();
        private readonly Dictionary<Type, Type> _fieldCodecs = new();
        private readonly Dictionary<Type, Type> _copiers = new();
        private readonly Dictionary<Type, Type> _baseCopiers = new();
        private readonly Dictionary<Type, Type> _activators = new();
        private readonly List<IGeneralizedCodec> _generalizedCodecs = new();
        private readonly List<ISpecializableCodec> _specializableCodecs = new();
        private readonly List<IGeneralizedCopier> _generalizedCopiers = new();
        private readonly List<ISpecializableCopier> _specializableCopiers = new();
        private readonly VoidCodec _voidCodec = new();
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
                _specializableCodecs.AddRange(_serviceProvider.GetServices<ISpecializableCodec>());
                _specializableCopiers.AddRange(_serviceProvider.GetServices<ISpecializableCopier>());
            }
        }

        private void ConsumeMetadata(IConfiguration<SerializerConfiguration> codecConfiguration)
        {
            var metadata = codecConfiguration.Value;
            Func<Type, bool> noFilter = _ => true;
            AddFromMetadata(_baseCodecs, metadata.Serializers, typeof(IBaseCodec<>), noFilter);
            AddFromMetadata(_valueSerializers, metadata.Serializers, typeof(IValueSerializer<>), noFilter);
            AddFromMetadata(_fieldCodecs, metadata.Serializers, typeof(IFieldCodec<>), noFilter);
            AddFromMetadata(_fieldCodecs, metadata.FieldCodecs, typeof(IFieldCodec<>), noFilter);
            AddFromMetadata(_activators, metadata.Activators, typeof(IActivator<>), noFilter);
            AddFromMetadata(_copiers, metadata.Copiers, typeof(IDeepCopier<>), noFilter);
            AddFromMetadata(_baseCopiers, metadata.Copiers, typeof(IBaseCopier<>), noFilter);

            static void AddFromMetadata(IDictionary<Type, Type> resultCollection, IEnumerable<Type> metadataCollection, Type genericType, Func<Type, bool> predicate)
            {
                Debug.Assert(genericType.GetGenericArguments().Length == 1);

                foreach (var type in metadataCollection)
                {
                    if (!predicate(type))
                    {
                        continue;
                    }

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
                }

                if (untypedResult is null)
                {
                    foreach (var specializableCodec in _specializableCodecs)
                    {
                        if (specializableCodec.IsSupportedType(fieldType))
                        {
                            untypedResult = specializableCodec.GetSpecializedCodec(fieldType);
                        }
                    }
                }

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

        public IBaseCodec<TField> GetBaseCodec<TField>() where TField : class
        {
            if (!_initialized)
            {
                Initialize();
            }

            ThrowIfUnsupportedType(typeof(TField));
            var type = typeof(TField);
            var searchType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            return GetBaseCodecInner<TField>(type, searchType) ?? ThrowBaseCodecNotFound<TField>(type);
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

        public IBaseCopier<TField> GetBaseCopier<TField>() where TField : class
        {
            if (!_initialized)
            {
                Initialize();
            }

            ThrowIfUnsupportedType(typeof(TField));
            var type = typeof(TField);
            var searchType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            return GetBaseCopierInner<TField>(type, searchType) ?? ThrowBaseCopierNotFound<TField>(type);
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
                }

                if (untypedResult is null)
                {
                    foreach (var specializableCopier in _specializableCopiers)
                    {
                        if (specializableCopier.IsSupportedType(fieldType))
                        {
                            untypedResult = specializableCopier.GetSpecializedCodec(fieldType);
                            break;
                        }
                    }
                }

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

        private IBaseCodec<TField> GetBaseCodecInner<TField>(Type concreteType, Type searchType) where TField : class
        {
            if (!_baseCodecs.TryGetValue(searchType, out var serializerType))
            {
                return null;
            }

            if (serializerType.IsGenericTypeDefinition)
            {
                serializerType = serializerType.MakeGenericType(concreteType.GetGenericArguments());
            }

            if (!_instantiatedBaseCodecs.TryGetValue(serializerType, out var result))
            {
                result = GetServiceOrCreateInstance(serializerType);
                _ = _instantiatedBaseCodecs.TryAdd(serializerType, result);
            }

            return (IBaseCodec<TField>)result;
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

        private IBaseCopier<T> GetBaseCopierInner<T>(Type concreteType, Type searchType) where T : class
        {
            if (!_baseCopiers.TryGetValue(searchType, out var copierType))
            {
                return null;
            }

            if (copierType.IsGenericTypeDefinition)
            {
                copierType = copierType.MakeGenericType(concreteType.GetGenericArguments());
            }

            if (!_instantiatedBaseCopiers.TryGetValue(copierType, out var result))
            {
                result = GetServiceOrCreateInstance(copierType);
                _ = _instantiatedBaseCopiers.TryAdd(copierType, result);
            }

            return (IBaseCopier<T>)result;
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
            else if (_baseCodecs.TryGetValue(searchType, out var baseCodecType))
            {
                if (baseCodecType.IsGenericTypeDefinition)
                {
                    baseCodecType = baseCodecType.MakeGenericType(fieldType.GetGenericArguments());
                }

                // If there is a partial serializer for this type, create a codec which will then accept that partial serializer.
                codecType = typeof(ConcreteTypeSerializer<,>).MakeGenericType(fieldType, baseCodecType);
                constructorArguments = new[] { GetServiceOrCreateInstance(baseCodecType) };
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
            else if (searchType.BaseType is object && CreateCodecInstance(fieldType, searchType.BaseType) is IFieldCodec fieldIdDelta)
            {
                // Find codecs which generalize over all subtypes.
                if (fieldIdDelta is IDerivedTypeCodec)
                {
                    return fieldIdDelta;
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
            else if (_baseCopiers.TryGetValue(searchType, out var baseCopierType))
            {
                if (baseCopierType.IsGenericTypeDefinition)
                {
                    baseCopierType = baseCopierType.MakeGenericType(fieldType.GetGenericArguments());
                }

                // If there is a partial serializer for this type, create a copier which will then accept that partial serializer.
                copierType = typeof(ConcreteTypeCopier<,>).MakeGenericType(fieldType, baseCopierType);
                constructorArguments = new[] { GetServiceOrCreateInstance(baseCopierType) };
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
        private static IBaseCodec<TField> ThrowBaseCodecNotFound<TField>(Type fieldType) where TField : class => throw new KeyNotFoundException($"Could not find a partial serializer for type {fieldType}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IValueSerializer<TField> ThrowValueSerializerNotFound<TField>(Type fieldType) where TField : struct => throw new KeyNotFoundException($"Could not find a value serializer for type {fieldType}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IActivator<T> ThrowActivatorNotFound<T>(Type type) => throw new KeyNotFoundException($"Could not find an activator for type {type}.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IBaseCopier<T> ThrowBaseCopierNotFound<T>(Type type) where T : class => throw new KeyNotFoundException($"Could not find a partial copier for type {type}.");
    }
}