using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Supports the serialization of inaccessible implementations of abstract types, such as RuntimeType.
    /// </summary>
    /// <typeparam name="TConcrete">The inaccessible implementation type.</typeparam>
    /// <typeparam name="TAbstract">The abstract base type.</typeparam>
    /// <typeparam name="TAbstractCodec">The codec for the abstract base type.</typeparam>
    internal sealed class AbstractCodecAdapter<TConcrete, TAbstract, TAbstractCodec> : IFieldCodec<TConcrete>
        where TConcrete : TAbstract where TAbstractCodec : IFieldCodec<TAbstract>
    {
        private readonly TAbstractCodec codec;

        public AbstractCodecAdapter(TAbstractCodec codec)
        {
            this.codec = codec;
        }

        /// <inheritdoc />
        public void WriteField<TBufferWriter>(
            ref Writer<TBufferWriter> writer,
            uint fieldIdDelta,
            Type expectedType,
            TConcrete value) where TBufferWriter : IBufferWriter<byte> =>
            this.codec.WriteField(ref writer, fieldIdDelta, expectedType, value);

        /// <inheritdoc />
        public TConcrete ReadValue(ref Reader reader, Field field) => (TConcrete)this.codec.ReadValue(ref reader, field);
    }
}