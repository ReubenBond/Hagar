using Hagar.Buffers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Hagar.Serializers
{
    /// <summary>
    /// Serializes the content of a value type without framing the type itself.
    /// </summary>
    /// <typeparam name="T">The type which this implementation can serialize and deserialize.</typeparam>
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IValueSerializer<T> where T : struct
    {
        void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, ref T value) where TBufferWriter : IBufferWriter<byte>;
        void Deserialize<TInput>(ref Reader<TInput> reader, ref T value);
    }
}