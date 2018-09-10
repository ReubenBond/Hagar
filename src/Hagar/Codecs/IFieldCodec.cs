using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public interface IFieldCodec
    {
    }

    public interface IFieldCodec<T> : IFieldCodec
    {
        void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, T value) where TBufferWriter : IBufferWriter<byte>;
        T ReadValue(ref Reader reader, Field field);
    }
}