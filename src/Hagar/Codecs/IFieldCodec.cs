using Hagar.Buffers;
using Hagar.WireProtocol;
using System;
using System.Buffers;

namespace Hagar.Codecs
{
    public interface IFieldCodec
    {
    }

    public interface IFieldCodec<T> : IFieldCodec
    {
        void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, T value) where TBufferWriter : IBufferWriter<byte>;
        T ReadValue<TInput>(ref Reader<TInput> reader, Field field);
    }

    /// <summary>
    /// Marker interface for codecs which directly support serializing all derived types of their specified type.
    /// </summary>
    public interface IDerivedTypeCodec
    {
    }
}