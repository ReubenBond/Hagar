using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.Utilities;
using Microsoft.Extensions.DependencyInjection;
namespace Hagar.Serializers
{
    public class CodecProvider : ITypedCodecProvider, IUntypedCodecProvider, IPartialSerializerProvider
    {
        private static readonly Type ObjectType = typeof(object);
        private static readonly Type OpenGenericCodecType = typeof(IFieldCodec<>);
        private static readonly MethodInfo TypedCodecWrapperCreateMethod = typeof(CodecAdapter).GetMethod(nameof(CodecAdapter.CreateUntypedFromTyped), BindingFlags.Public | BindingFlags.Static);

        private readonly object initializationLock = new object();
        private readonly CachedReadConcurrentDictionary<(Type, Type), IFieldCodec> adaptedCodecs = new CachedReadConcurrentDictionary<(Type, Type), IFieldCodec>();
        private readonly Dictionary<Type, Type> partialSerializers = new Dictionary<Type, Type>();
        private readonly Dictionary<Type, Type> fieldCodecs = new Dictionary<Type, Type>();
        private readonly List<IGeneralizedCodec> generalized = new List<IGeneralizedCodec>();
        private readonly VoidCodec voidCodec = new VoidCodec();
        private readonly IServiceProvider serviceProvider;
        private bool initialized;

        public CodecProvider(IServiceProvider serviceProvider, IConfiguration<SerializerConfiguration> codecConfiguration)
        {
            this.serviceProvider = serviceProvider;
            this.fieldCodecs[typeof(object)] = typeof(ObjectCodec);
            
            // ReSharper disable once PossibleMistakenCallToGetType.2
            this.fieldCodecs[typeof(Type).GetType()] = typeof(TypeSerializerCodec);
            this.ConsumeMetadata(codecConfiguration);
        }
        
        private void Initialize()
        {
            if (this.initialized) return;
            lock (this.initializationLock)
            {
                if (this.initialized) return;

                this.initialized = true;
                this.generalized.AddRange(this.serviceProvider.GetServices<IGeneralizedCodec>());
            }
        }

        private void ConsumeMetadata(IConfiguration<SerializerConfiguration> codecConfiguration)
        {
            var metadata = codecConfiguration.Value;
            AddFromMetadata(this.partialSerializers, metadata.PartialSerializers, typeof(IPartialSerializer<>));
            AddFromMetadata(this.fieldCodecs, metadata.FieldCodecs, typeof(IFieldCodec<>));
            
            void AddFromMetadata(Dictionary<Type, Type> resultCollection, IEnumerable<Type> metadataCollection, Type genericType)
            {
                if (genericType.GetGenericArguments().Length != 1) throw new ArgumentException($"Type {genericType} must have an arity of 1.");

                foreach (var fieldCodec in metadataCollection)
                {
                    foreach (var iface in fieldCodec.GetInterfaces())
                    {
                        if (!iface.IsGenericType) continue;
                        if (genericType != iface.GetGenericTypeDefinition()) continue;
                        var genericArgument = iface.GetGenericArguments()[0];
                        if (typeof(object) == genericArgument) continue;
                        if (genericArgument.IsConstructedGenericType && genericArgument.GenericTypeArguments.Any(arg => arg.IsGenericParameter))
                        {
                            genericArgument = genericArgument.GetGenericTypeDefinition();
                        }
                        resultCollection[genericArgument] = fieldCodec;
                    }
                }
            }
        }

        public IFieldCodec<TField> TryGetCodec<TField>() => this.TryGetCodec<TField>(typeof(TField));

        public IFieldCodec<object> GetCodec(Type fieldType) => this.TryGetCodec(fieldType) ?? ThrowCodecNotFound<object>(fieldType);

        public IFieldCodec<object> TryGetCodec(Type fieldType) => this.TryGetCodec<object>(fieldType);

        public IFieldCodec<TField> GetCodec<TField>() => this.TryGetCodec<TField>() ?? ThrowCodecNotFound<TField>(typeof(TField));

        private IFieldCodec<TField> TryGetCodec<TField>(Type fieldType)
        {
            if (!this.initialized) this.Initialize();
            var resultFieldType = typeof(TField);
            bool wasCreated = false;

            // Try to find the codec from the configured codecs.
            IFieldCodec untypedResult;

            // If the field type is unavailable, return the void codec which can at least handle references.
            // TODO: Is there a more appropriate codec, eg to consume fields?
            if (fieldType == null) untypedResult = this.voidCodec;
            else if (!this.adaptedCodecs.TryGetValue((fieldType, resultFieldType), out untypedResult))
            {
                ThrowIfUnsupportedType(fieldType);

                if (fieldType.IsConstructedGenericType)
                {
                    untypedResult = this.CreateCodecInstance(fieldType, fieldType.GetGenericTypeDefinition());
                }
                else
                {
                    untypedResult = this.CreateCodecInstance(fieldType, fieldType);
                    if (untypedResult == null)
                    {
                        if (!this.initialized) this.Initialize();
                        foreach (var dynamicCodec in this.generalized)
                        {
                            if (dynamicCodec.IsSupportedType(fieldType))
                            {
                                untypedResult = dynamicCodec;
                                break;
                            }
                        }
                    }
                }

                if (untypedResult == null && (fieldType.IsInterface || fieldType.IsAbstract))
                {
                    untypedResult = (IFieldCodec) ActivatorUtilities.GetServiceOrCreateInstance(
                        this.serviceProvider,
                        typeof(AbstractTypeSerializer<>).MakeGenericType(fieldType));
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
                typedResult = (IFieldCodec<TField>) this.adaptedCodecs.GetOrAdd((fieldType, resultFieldType), _ => untypedResult);
            }
            else if (typedResult == null)
            {
                ThrowCannotConvert(untypedResult);
            }

            return typedResult;

            IFieldCodec<TField> TryWrapCodec(object rawCodec)
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

            void ThrowCannotConvert(object rawCodec)
            {
                throw new InvalidOperationException($"Cannot convert codec of type {rawCodec.GetType()} to codec of type {typeof(IFieldCodec<TField>)}.");
            }
        }
        
        public IPartialSerializer<TField> GetPartialSerializer<TField>() where TField : class
        {
            if (!this.initialized) this.Initialize();
            ThrowIfUnsupportedType(typeof(TField));
            var type = typeof(TField);
            var searchType = type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

            return this.GetPartialSerializerInner<TField>(type, searchType);
        }

        private IPartialSerializer<TField> GetPartialSerializerInner<TField>(Type concreteType, Type searchType) where TField : class
        {
            if (!this.partialSerializers.TryGetValue(searchType, out var serializerType)) return null;
            if (serializerType.IsGenericTypeDefinition) serializerType = serializerType.MakeGenericType(concreteType.GetGenericArguments());
            return (IPartialSerializer<TField>)ActivatorUtilities.GetServiceOrCreateInstance(this.serviceProvider, serializerType);
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

        private IFieldCodec CreateCodecInstance(Type fieldType, Type searchType)
        {
            IFieldCodec untypedResult = null;
            if (this.fieldCodecs.TryGetValue(searchType, out var codecType))
            {
                if (codecType.IsGenericTypeDefinition) codecType = codecType.MakeGenericType(fieldType.GetGenericArguments());
                untypedResult = (IFieldCodec) ActivatorUtilities.GetServiceOrCreateInstance(this.serviceProvider, codecType);
            }
            else if (this.partialSerializers.TryGetValue(searchType, out var serializerType))
            {
                if (serializerType.IsGenericTypeDefinition) serializerType = serializerType.MakeGenericType(fieldType.GetGenericArguments());
                var partialSerializer = ActivatorUtilities.GetServiceOrCreateInstance(this.serviceProvider, serializerType);
                untypedResult = (IFieldCodec) ActivatorUtilities.CreateInstance(
                    this.serviceProvider,
                    typeof(ConcreteTypeSerializer<>).MakeGenericType(fieldType),
                    partialSerializer);
            }
            else if (fieldType.IsArray)
            {
                Type arrayCodecType;
                if (fieldType.GetArrayRank() == 1)
                {
                    arrayCodecType = typeof(ArrayCodec<>).MakeGenericType(fieldType.GetElementType());
                }
                else
                {
                    arrayCodecType = typeof(MultiDimensionalArrayCodec<>).MakeGenericType(fieldType.GetElementType());
                }

                untypedResult = (IFieldCodec) ActivatorUtilities.CreateInstance(this.serviceProvider, arrayCodecType);
            }

            return untypedResult;
        }

        private static void ThrowPointerType(Type fieldType)
        {
            throw new NotSupportedException($"Type {fieldType} is a pointer type and is therefore not supported.");
        }

        private static void ThrowByRefType(Type fieldType)
        {
            throw new NotSupportedException($"Type {fieldType} is a by-ref type and is therefore not supported.");
        }
        
        private static void ThrowGenericTypeDefinition(Type fieldType)
        {
            throw new InvalidOperationException($"Type {fieldType} is a non-constructed generic type and is therefore unsupported.");
        }

        // TODO: Use a library-specific exception.
        private static IFieldCodec<TField> ThrowCodecNotFound<TField>(Type fieldType) => throw new KeyNotFoundException($"Could not find a codec for type {fieldType}.");
    }

    public interface IPartialSerializerProvider
    {
        IPartialSerializer<TField> GetPartialSerializer<TField>() where TField : class;
    }
}