using Hagar.Buffers;
using System.Buffers;

namespace Hagar.Serializers
{
    public interface IPartialSerializer<T> : IPartialSerializer where T : class
    {
        void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, T value) where TBufferWriter : IBufferWriter<byte>;
        void Deserialize<TInput>(ref Reader<TInput> reader, T value);
    }

    public interface IPartialSerializer
    {
    }
}