using System;
using Hagar.Buffers;
using Hagar.Session;

namespace Hagar.ISerializable
{
    internal interface ISerializableSerializer
    {
        void WriteValue(Writer writer, SerializerSession session, object value);
        object ReadValue(Reader reader, SerializerSession session, Type type, uint placeholderReferenceId);
    }
}