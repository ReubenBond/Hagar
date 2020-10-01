using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.WireProtocol;
using System;
using System.Collections.Generic;

namespace Hagar.Serializers
{
    /// <summary>
    /// Serializer for reference types which can be instantiated.
    /// </summary>
    /// <typeparam name="TField">The field type.</typeparam>
    /// <typeparam name="TPartialSerializer">The partial serializer implementation type.</typeparam>
    public sealed class ConcreteTypeSerializer<TField, TPartialSerializer> : IFieldCodec<TField> where TField : class where TPartialSerializer : IPartialSerializer<TField>
    {
        private static readonly Type CodecFieldType = typeof(TField);
        private readonly IActivator<TField> _activator;
        private readonly TPartialSerializer _serializer;

        public ConcreteTypeSerializer(IActivator<TField> activator, TPartialSerializer serializer)
        {
            this._activator = activator;
            this._serializer = serializer;
        }

        void IFieldCodec<TField>.WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TField value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
            {
                return;
            }

            var fieldType = value.GetType();
            if (fieldType == CodecFieldType)
            {
                writer.WriteStartObject(fieldIdDelta, expectedType, fieldType);
                _serializer.Serialize(ref writer, value);
                writer.WriteEndObject();
            }
            else
            {
                var specificSerializer = writer.Session.CodecProvider.GetCodec(fieldType);
                if (specificSerializer != null)
                {
                    specificSerializer.WriteField(ref writer, fieldIdDelta, expectedType, value);
                }
                else
                {
                    ThrowSerializerNotFoundException(fieldType);
                }
            }
        }

        TField IFieldCodec<TField>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
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

            // The type is a descendant, not an exact match, so get the specific serializer for it.
            var specificSerializer = reader.Session.CodecProvider.GetCodec(fieldType);
            if (specificSerializer != null)
            {
                return (TField)specificSerializer.ReadValue(ref reader, field);
            }

            ThrowSerializerNotFoundException(fieldType);
            return null;
        }

        private static void ThrowSerializerNotFoundException(Type type) => throw new KeyNotFoundException($"Could not find a serializer of type {type}.");
    }
}