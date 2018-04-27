using System;
using System.Collections.Generic;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Serializers
{
    /// <summary>
    /// Serializer for types which are abstract and therefore cannot be instantiated themselves, such as abstract classes and interface types.
    /// </summary>
    /// <typeparam name="TField"></typeparam>
    public class AbstractTypeSerializer<TField> : IFieldCodec<TField> where TField : class
    {
        private readonly IUntypedCodecProvider codecProvider;

        public AbstractTypeSerializer(IUntypedCodecProvider codecProvider)
        {
            this.codecProvider = codecProvider;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, TField value)
        {
            // If the value is null then we will not be able to get its type in order to get a concrete codec for it.
            // Therefore write the null reference and exit.
            if (value is null)
            {
                ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, null);
                return;
            }

            var fieldType = value.GetType();
            var specificSerializer = this.codecProvider.GetCodec(fieldType);
            if (specificSerializer != null)
            {
                specificSerializer.WriteField(writer, session, fieldIdDelta, expectedType, value);
            }
            else
            {
                ThrowSerializerNotFound(fieldType);
            }
        }

        public TField ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<TField>(reader, session, field, this.codecProvider);
            var fieldType = field.FieldType;
            if (fieldType == null) ThrowMissingFieldType();

            var specificSerializer = this.codecProvider.GetCodec(fieldType);
            if (specificSerializer != null)
            {
                return (TField)specificSerializer.ReadValue(reader, session, field);
            }

            return ThrowSerializerNotFound(fieldType);
        }

        private static TField ThrowSerializerNotFound(Type type)
        {
            throw new KeyNotFoundException($"Could not find a serializer of type {type}.");
        }
        
        private static void ThrowMissingFieldType()
        {
            throw new FieldTypeMissingException(typeof(TField));
        }
    }
}