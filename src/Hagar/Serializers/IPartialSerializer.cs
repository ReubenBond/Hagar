using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Hagar.Buffers;

namespace Hagar.Serializers
{
    /// <summary>
    /// Serializer the content of a specified type without framing the type itself.
    /// </summary>
    /// <typeparam name="T">The type which this implementation can serialize and deserialize.</typeparam>
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IPartialSerializer<T> where T : class
    {
        void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, T value) where TBufferWriter : IBufferWriter<byte>;
        void Deserialize(ref Reader reader, T value);
    }
}