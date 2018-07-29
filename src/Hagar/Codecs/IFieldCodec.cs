using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public interface IFieldCodec
    {
    }

    public interface IFieldCodec<T> : IFieldCodec
    {
        void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, T value);
        T ReadValue(ref Reader reader, SerializerSession session, Field field);
    }
}