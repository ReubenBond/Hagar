using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public static class ReferenceCodec
    {
        /// <summary>
        /// Indicates that the field being serialized or deserialized is a value type.
        /// </summary>
        /// <param name="session">The serializer session.</param>
        public static void MarkValueField(SerializerSession session)
        {
            session.ReferencedObjects.MarkValueField();
        }

        public static bool TryWriteReferenceField(
            ref Writer writer,
            SerializerSession session,
            uint fieldId,
            Type expectedType,
            object value)
        {
            if (!session.ReferencedObjects.GetOrAddReference(value, out uint reference))
            {
                return false;
            }

            writer.WriteFieldHeader(session, fieldId, expectedType, value?.GetType(), WireType.Reference);
            writer.WriteVarInt(reference);
            return true;
        }

        public static T ReadReference<T>(ref Reader reader, SerializerSession session, Field field)
        {
            return (T) ReadReference(ref reader, session, field, typeof(T));
        }

        public static object ReadReference(ref Reader reader, SerializerSession session, Field field, Type expectedType)
        {
            var reference = reader.ReadVarUInt32();
            if (!session.ReferencedObjects.TryGetReferencedObject(reference, out object value))
            {
                ThrowReferenceNotFound(expectedType, reference);
            }

            switch (value)
            {
                case UnknownFieldMarker marker:
                    return DeserializeFromMarker(ref reader, session, field, marker, reference, expectedType);
                default:
                    return value;
            }
        }

        private static object DeserializeFromMarker(
            ref Reader reader,
            SerializerSession session,
            Field field,
            UnknownFieldMarker marker,
            uint reference,
            Type lastResortFieldType)
        {
            // Create a reader at the position specified by the marker.
            var referencedReader = reader.ForkFrom(marker.Position);

            // Determine the correct type for the field.
            var fieldType = marker.Field.FieldType ?? field.FieldType ?? lastResortFieldType;

            // Get a serializer for that type.
            var specificSerializer = session.CodecProvider.GetCodec(fieldType);

            // Reset the session's reference id so that the deserialized object overwrites the marker.
            var originalCurrentReferenceId = session.ReferencedObjects.CurrentReferenceId;
            session.ReferencedObjects.CurrentReferenceId = reference - 1;

            // Deserialize the object, replacing the marker in the session.
            try
            {
                return specificSerializer.ReadValue(ref referencedReader, session, marker.Field);
            }
            finally
            {
                // Revert the reference id.
                session.ReferencedObjects.CurrentReferenceId = originalCurrentReferenceId;
            }
        }

        public static void RecordObject(SerializerSession session, object value) => session.ReferencedObjects.RecordReferenceField(value);
        public static void RecordObject(SerializerSession session, object value, uint referenceId) => session.ReferencedObjects.RecordReferenceField(value, referenceId);

        /// <summary>
        /// Records and returns a placeholder reference id for objects which cannot be immediately deserialized.
        /// </summary>
        public static uint CreateRecordPlaceholder(SerializerSession session)
        {
            var referencedObject = session.ReferencedObjects;
            return ++referencedObject.CurrentReferenceId;
        }

        private static void ThrowReferenceNotFound(Type expectedType, uint reference)
        {
            throw new ReferenceNotFoundException(expectedType, reference);
        }
    }
}