using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public static class ReferenceCodec
    {
        /// <summary>
        /// Indicates that the field being serialized or deserialized is a value type.
        /// </summary>
        /// <param name="session">The serializer session.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarkValueField(SerializerSession session)
        {
            session.ReferencedObjects.MarkValueField();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteReferenceField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldId,
            Type expectedType,
            object value) where TBufferWriter : IBufferWriter<byte>
        {
            if (!writer.Session.ReferencedObjects.GetOrAddReference(value, out var reference))
            {
                return false;
            }

            writer.WriteFieldHeader(fieldId, expectedType, value?.GetType(), WireType.Reference);
            writer.WriteVarInt(reference);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadReference<T>(ref Reader reader, Field field)
        {
            return (T) ReadReference(ref reader, field, typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object ReadReference(ref Reader reader, Field field, Type expectedType)
        {
            var reference = reader.ReadVarUInt32();
            if (!reader.Session.ReferencedObjects.TryGetReferencedObject(reference, out var value))
            {
                ThrowReferenceNotFound(expectedType, reference);
            }

            switch (value)
            {
                case UnknownFieldMarker marker:
                    return DeserializeFromMarker(ref reader, field, marker, reference, expectedType);
                default:
                    return value;
            }
        }

        private static object DeserializeFromMarker(
            ref Reader reader,
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
            var session = reader.Session;
            var specificSerializer = session.CodecProvider.GetCodec(fieldType);

            // Reset the session's reference id so that the deserialized object overwrites the marker.
            var originalCurrentReferenceId = session.ReferencedObjects.CurrentReferenceId;
            session.ReferencedObjects.CurrentReferenceId = reference - 1;

            // Deserialize the object, replacing the marker in the session.
            try
            {
                return specificSerializer.ReadValue(ref referencedReader, marker.Field);
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