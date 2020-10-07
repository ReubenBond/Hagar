using Hagar.Buffers;
using System;
using System.Buffers;

namespace Hagar.ISerializable
{
    internal interface ISerializableSerializer
    {
        void WriteValue<TBufferWriter>(ref Writer<TBufferWriter> writer, object value) where TBufferWriter : IBufferWriter<byte>;
        object ReadValue<TInput>(ref Reader<TInput> reader, Type type, uint placeholderReferenceId);
    }
}