using Hagar.Buffers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Hagar.Serializers
{
    public interface IPartialSerializer<T> where T : class
    {
        void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, T value) where TBufferWriter : IBufferWriter<byte>;
        void Deserialize<TInput>(ref Reader<TInput> reader, T value);
    }
}