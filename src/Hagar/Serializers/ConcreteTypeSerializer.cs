using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar.Serializers
{
    /// <summary>
    /// Serializer for reference types which can be instantiated.
    /// </summary>
    /// <typeparam name="TField">The field type.</typeparam>
    /// <typeparam name="TBaseCodec">The partial serializer implementation type.</typeparam>
    public sealed class ConcreteTypeSerializer<TField, TBaseCodec> : IFieldCodec<TField> where TField : class where TBaseCodec : IBaseCodec<TField>
    {
        private static readonly Type CodecFieldType = typeof(TField);
        private readonly IActivator<TField> _activator;
        private readonly TBaseCodec _serializer;

        public ConcreteTypeSerializer(IActivator<TField> activator, TBaseCodec serializer)
        {
            _activator = activator;
            _serializer = serializer;
        }

        public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TField value) where TBufferWriter : IBufferWriter<byte>
        {
            var fieldType = value?.GetType();
            if (fieldType is null || fieldType == CodecFieldType)
            {
                if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
                {
                    return;
                }

                writer.WriteStartObject(fieldIdDelta, expectedType, fieldType);
                _serializer.Serialize(ref writer, value);
                writer.WriteEndObject();
            }
            else
            {
                HagarGeneratedCodeHelper.SerializeUnexpectedType(ref writer, fieldIdDelta, expectedType, value);
            }
        }

        public TField ReadValue<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<TField, TInput>(ref reader, field);
            }

            var fieldType = field.FieldType;
            if (fieldType is null || fieldType == CodecFieldType)
            {
                var result = _activator.Create();
                ReferenceCodec.RecordObject(reader.Session, result);
                _serializer.Deserialize(ref reader, result);
                return result;
            }

            return HagarGeneratedCodeHelper.DeserializeUnexpectedType<TInput, TField>(ref reader, field);
        }

        public TField ReadValueSealed<TInput>(ref Reader<TInput> reader, Field field)
        {
            if (field.WireType == WireType.Reference)
            {
                return ReferenceCodec.ReadReference<TField, TInput>(ref reader, field);
            }

            var result = _activator.Create();
            ReferenceCodec.RecordObject(reader.Session, result);
            _serializer.Deserialize(ref reader, result);
            return result;
        }
    }
}