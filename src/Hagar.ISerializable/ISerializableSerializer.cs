using System;
using Hagar.Buffers;
using Hagar.Session;

namespace Hagar.ISerializable
{
    internal interface ISerializableSerializer
    {
        void WriteValue(ref Writer writer, SerializerSession session, object value);
        object ReadValue(ref Reader reader, SerializerSession session, Type type, uint placeholderReferenceId);
    }
}