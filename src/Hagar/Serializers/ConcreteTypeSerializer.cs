using System;
using System.Collections.Generic;
using Hagar.Activators;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Serializers
{
    /// <summary>
    /// Serializer for reference types which can be instantiated.
    /// </summary>
    /// <typeparam name="TField">The field type.</typeparam>
    public class ConcreteTypeSerializer<TField> : IFieldCodec<TField> where TField : class
    {
        private static readonly Type CodecFieldType = typeof(TField);
        private readonly IActivator<TField> activator;
        private readonly IUntypedCodecProvider codecProvider;
        private readonly IPartialSerializer<TField> serializer;

        public ConcreteTypeSerializer(IActivator<TField> activator, IUntypedCodecProvider codecProvider, IPartialSerializerProvider partialSerializerProvider)
        {
            this.activator = activator;
            this.codecProvider = codecProvider;
            this.serializer = partialSerializerProvider.GetPartialSerializer<TField>();
        }

        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, TField value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;
            var fieldType = value.GetType();
            if (fieldType == CodecFieldType)
            {
                writer.WriteStartObject(session, fieldIdDelta, expectedType, fieldType);
                this.serializer.Serialize(ref writer, session, value);

                writer.WriteEndObject();
            }
            else
            {
                var specificSerializer = this.codecProvider.GetCodec(fieldType);
                if (specificSerializer != null)
                {
                    specificSerializer.WriteField(ref writer, session, fieldIdDelta, expectedType, value);
                }
                else
                {
                    ThrowSerializerNotFoundException(fieldType);
                }
            }
        }

        public TField ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<TField>(ref reader, session, field, this.codecProvider);
            var fieldType = field.FieldType;
            if (fieldType == null || fieldType == CodecFieldType)
            {
                var result = this.activator.Create();
                ReferenceCodec.RecordObject(session, result);
                this.serializer.Deserialize(ref reader, session, result);
                return result;
            }

            // The type is a descendant, not an exact match, so get the specific serializer for it.
            var specificSerializer = this.codecProvider.GetCodec(fieldType);
            if (specificSerializer != null)
            {
                return (TField)specificSerializer.ReadValue(ref reader, session, field);
            }

            ThrowSerializerNotFoundException(fieldType);
            return null;
        }

        private static void ThrowSerializerNotFoundException(Type type)
        {
            throw new KeyNotFoundException($"Could not find a serializer of type {type}.");
        }
    }
}