using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.Session;

namespace Hagar.ISerializable
{
    internal interface ISerializableSerializer
    {
        void WriteValue<TBufferWriter>(ref Writer<TBufferWriter> writer, SerializerSession session, object value) where TBufferWriter : IBufferWriter<byte>;
        object ReadValue(ref Reader reader, SerializerSession session, Type type, uint placeholderReferenceId);
    }
}