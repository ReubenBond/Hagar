using Hagar.Buffers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Hagar.Serializers
{
    public interface IValueSerializer<T> where T : struct
    {
        void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, ref T value) where TBufferWriter : IBufferWriter<byte>;
        void Deserialize<TInput>(ref Reader<TInput> reader, ref T value);
    }
}