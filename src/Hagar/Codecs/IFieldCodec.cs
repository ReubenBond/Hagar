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
        void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, T value);
        T ReadValue(Reader reader, SerializerSession session, Field field);
    }
}