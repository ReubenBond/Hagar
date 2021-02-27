using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.TypeSystem;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

namespace Hagar.ISerializable
{
    public class DotNetSerializableCodec : IGeneralizedCodec
    {
        private const int ExceptionFlag = 1;
        public static readonly Type CodecType = typeof(DotNetSerializableCodec);
        private static readonly TypeInfo SerializableType = typeof(System.Runtime.Serialization.ISerializable).GetTypeInfo();
        private readonly IFieldCodec<Type> _typeCodec;
        private readonly TypeConverter _typeConverter;
        private readonly StreamingContext _streamingContext;
        private readonly ObjectSerializer _objectSerializer;
        private readonly ValueTypeSerializerFactory _valueTypeSerializerFactory;

        public DotNetSerializableCodec(IFieldCodec<Type> typeCodec, TypeConverter typeResolver)
        {
            _streamingContext = new StreamingContext(StreamingContextStates.All);
            _typeCodec = typeCodec;
            _typeConverter = typeResolver;
            var entrySerializer = new SerializationEntryCodec();
            var constructorFactory = new SerializationConstructorFactory();
            var serializationCallbacks = new SerializationCallbacksFactory();
            var formatterConverter = new FormatterConverter();

            _objectSerializer = new ObjectSerializer(
                entrySerializer,
                constructorFactory,
                serializationCallbacks,
                formatterConverter,
                _streamingContext);

            _valueTypeSerializerFactory = new ValueTypeSerializerFactory(
                entrySerializer,
                constructorFactory,
                serializationCallbacks,
                formatterConverter,
                _streamingContext);
        }

        [SecurityCritical]
        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, object value) where TBufferWriter : IBufferWriter<byte>
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            var type = value.GetType();
            writer.WriteFieldHeader(fieldIdDelta, expectedType, CodecType, WireType.TagDelimited);

            if (typeof(Exception).IsAssignableFrom(type))
            {
                // Indicate that this is an exception
                Int32Codec.WriteField(ref writer, 0, typeof(int), ExceptionFlag);

                // For exceptions, serialize the name as a string, since the deserializing side may not 
                // have access to the specific exception type, but should be able to reconstruct some
                // alternative.
                var typeName = _typeConverter.Format(type);
                StringCodec.WriteField(ref writer, 1, typeof(string), typeName);
            }
            else
            {
                Int32Codec.WriteField(ref writer, 0, typeof(int), 0);
                _typeCodec.WriteField(ref writer, 1, typeof(Type), type);
            }

            if (type.IsValueType)
            {
                var serializer = _valueTypeSerializerFactory.GetSerializer(type);
                serializer.WriteValue(ref writer, value);
            }
            else
            {
                _objectSerializer.WriteValue(ref writer, value);
            }

            writer.WriteEndObject();
        }

        [SecurityCritical]
        public object ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<object, TInput>(ref reader, field);
            }

            var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);

            var header = reader.ReadFieldHeader();
            var flags = Int32Codec.ReadValue(ref reader, header);
            if (flags == ExceptionFlag)
            {
                // This is an exception type, so deserialize it as an exception.
                header = reader.ReadFieldHeader();
                var typeName = StringCodec.ReadValue(ref reader, header);
                if (!_typeConverter.TryParse(typeName, out var type))
                {
                    // Deserialize into a fallback type for unknown exceptions
                    // This means that missing fields will not be represented.
                    var result = (UnavailableExceptionFallbackException)
                        _objectSerializer.ReadValue(
                            ref reader,
                            typeof(UnavailableExceptionFallbackException),
                            placeholderReferenceId);
                    result.ExceptionType = typeName;
                    return result;
                }

                return _objectSerializer.ReadValue(ref reader, type, placeholderReferenceId);
            }
            else
            {
                // This is a non-exception type.
                header = reader.ReadFieldHeader();
                var type = _typeCodec.ReadValue(ref reader, header);

                if (type.IsValueType)
                {
                    var serializer = _valueTypeSerializerFactory.GetSerializer(type);
                    return serializer.ReadValue(ref reader, type, placeholderReferenceId);
                }

                return _objectSerializer.ReadValue(ref reader, type, placeholderReferenceId);
            }
        }

        [SecurityCritical]
        public bool IsSupportedType(Type type) =>
            type == CodecType || SerializableType.IsAssignableFrom(type) && SerializationConstructorFactory.HasSerializationConstructor(type);
    }

    /// <summary>
    /// Represents an exception which has a type which is unavailable during deserialization.
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class UnavailableExceptionFallbackException : Exception
    {
        /// <inheritdoc />
        public UnavailableExceptionFallbackException()
        {
        }

        /// <inheritdoc />
        public UnavailableExceptionFallbackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            foreach (var pair in info)
            {
                Properties[pair.Name] = pair.Value;
            }
        }

        /// <inheritdoc />
        public UnavailableExceptionFallbackException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets the serialized properties of the exception.
        /// </summary>
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the exception type name.
        /// </summary>
        public string ExceptionType { get; internal set; }

        /// <inheritdoc />
        public override string ToString() => string.IsNullOrWhiteSpace(ExceptionType) ? $"Unknown exception: {base.ToString()}" : $"Unknown exception of type {ExceptionType}: {base.ToString()}";

        private string GetDebuggerDisplay() => ToString();
    }
}