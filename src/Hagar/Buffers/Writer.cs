using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
#if NETCOREAPP
using System.Numerics;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hagar.Buffers.Adaptors;
using Hagar.Session;
using Hagar.Utilities;

namespace Hagar.Buffers
{
    public static class Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<TBufferWriter> Create<TBufferWriter>(TBufferWriter destination, SerializerSession session) where TBufferWriter : IBufferWriter<byte> => new Writer<TBufferWriter>(destination, session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<MemoryStreamBufferWriter> Create(MemoryStream destination, SerializerSession session) => new Writer<MemoryStreamBufferWriter>(new MemoryStreamBufferWriter(destination), session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<PoolingStreamBufferWriter> CreatePooled(Stream destination, SerializerSession session, int sizeHint = 0) => new Writer<PoolingStreamBufferWriter>(new PoolingStreamBufferWriter(destination, sizeHint), session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<ArrayStreamBufferWriter> Create(Stream destination, SerializerSession session, int sizeHint = 0) => new Writer<ArrayStreamBufferWriter>(new ArrayStreamBufferWriter(destination, sizeHint), session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<ArrayBufferWriter> Create(byte[] output, SerializerSession session) => new Writer<ArrayBufferWriter>(new ArrayBufferWriter(output), session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<MemoryBufferWriter> Create(Memory<byte> output, SerializerSession session) => new Writer<MemoryBufferWriter>(new MemoryBufferWriter(output), session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer<SpanBufferWriter> Create(Span<byte> output, SerializerSession session) => new Writer<SpanBufferWriter>(new SpanBufferWriter(output), output, session);
    }

    public ref struct Writer<TBufferWriter> where TBufferWriter : IBufferWriter<byte>
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private TBufferWriter _output;
#pragma warning restore IDE0044 // Add readonly modifier
        private Span<byte> _currentSpan;
        private int _bufferPos;
        private int _previousBuffersSize;

        internal Writer(TBufferWriter output, SerializerSession session)
        {
            if (typeof(TBufferWriter) == typeof(SpanBufferWriter))
            {
                throw new NotSupportedException($"Type {typeof(TBufferWriter)} is not supported by this constructor");
            }
            else
            {
                _output = output;
                Session = session;
                _currentSpan = output.GetSpan();
                _bufferPos = default;
                _previousBuffersSize = default;
            }
        }

        internal Writer(TBufferWriter output, Span<byte> span, SerializerSession session)
        {
            if (typeof(TBufferWriter) == typeof(SpanBufferWriter))
            {
                _output = output;
                Session = session;
                _currentSpan = span;
                _bufferPos = default;
                _previousBuffersSize = default;
            }
            else
            {
                throw new NotSupportedException($"Type {typeof(TBufferWriter)} is not supported by this constructor");
            }
        }


        public SerializerSession Session { get; }

        public TBufferWriter Output => _output;

        public int Position => _previousBuffersSize + _bufferPos;

        public Span<byte> WritableSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentSpan.Slice(_bufferPos);
        }

        /// <summary>
        /// Advance the write position in the current span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceSpan(int length) => _bufferPos += length;

        /// <summary>
        /// Commit the currently written buffers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            _output.Advance(_bufferPos);
            _previousBuffersSize += _bufferPos;
            _currentSpan = default;
            _bufferPos = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureContiguous(int length)
        {
            // The current buffer is adequate.
            if (_bufferPos + length < _currentSpan.Length)
            {
                return;
            }

            // The current buffer is inadequate, allocate another.
            Allocate(length);
#if DEBUG
            // Throw if the allocation does not satisfy the request.
            if (_currentSpan.Length < length)
            {
                ThrowTooLarge(length);
            }

            static void ThrowTooLarge(int l) => throw new InvalidOperationException($"Requested buffer length {l} cannot be satisfied by the writer.");
#endif
        }

        public void Allocate(int length)
        {
            // Commit the bytes which have been written.
            _output.Advance(_bufferPos);

            // Request a new buffer with at least the requested number of available bytes.
            _currentSpan = _output.GetSpan(length);

            // Update internal state for the new buffer.
            _previousBuffersSize += _bufferPos;
            _bufferPos = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> value)
        {
            // Fast path, try copying to the current buffer.
            if (value.Length <= _currentSpan.Length - _bufferPos)
            {
                value.CopyTo(WritableSpan);
                _bufferPos += value.Length;
            }
            else
            {
                WriteMultiSegment(in value);
            }
        }

        private void WriteMultiSegment(in ReadOnlySpan<byte> source)
        {
            var input = source;
            while (true)
            {
                // Write as much as possible/necessary into the current segment.
                var writeSize = Math.Min(_currentSpan.Length - _bufferPos, input.Length);
                input.Slice(0, writeSize).CopyTo(WritableSpan);
                _bufferPos += writeSize;

                input = input.Slice(writeSize);

                if (input.Length == 0)
                {
                    return;
                }

                // The current segment is full but there is more to write.
                Allocate(input.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            EnsureContiguous(1);
            _currentSpan[_bufferPos++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            EnsureContiguous(1);
            _currentSpan[_bufferPos++] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value)
        {
            const int width = 2;
            EnsureContiguous(width);
            BinaryPrimitives.WriteInt16LittleEndian(WritableSpan, value);
            _bufferPos += width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            const int width = 4;
            EnsureContiguous(width);
            BinaryPrimitives.WriteInt32LittleEndian(WritableSpan, value);
            _bufferPos += width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            const int width = 8;
            EnsureContiguous(width);
            BinaryPrimitives.WriteInt64LittleEndian(WritableSpan, value);
            _bufferPos += width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            const int width = 4;
            EnsureContiguous(width);
            BinaryPrimitives.WriteUInt32LittleEndian(WritableSpan, value);
            _bufferPos += width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            const int width = 2;
            EnsureContiguous(width);
            BinaryPrimitives.WriteUInt16LittleEndian(WritableSpan, value);
            _bufferPos += width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            const int width = 8;
            EnsureContiguous(width);
            BinaryPrimitives.WriteUInt64LittleEndian(WritableSpan, value);
            _bufferPos += width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarInt(uint value)
        {
            // Since this method writes a ulong worth of bytes unconditionally, ensure that there is sufficient space.
            EnsureContiguous(sizeof(ulong));

            var pos = _bufferPos;
            var neededBytes = BitOperations.Log2(value) / 7;
            _bufferPos += neededBytes + 1;

            ulong lower = value;
            lower <<= 1;
            lower |= 0x01;
            lower <<= neededBytes;

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSpan), pos), lower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteVarInt(ulong value)
        {
            // Since this method writes a ulong plus a ushort worth of bytes unconditionally, ensure that there is sufficient space.
            EnsureContiguous(sizeof(ulong) + sizeof(ushort));

            var pos = _bufferPos;
            var neededBytes = BitOperations.Log2(value) / 7;
            _bufferPos += neededBytes + 1;

            ulong lower = value;
            lower <<= 1;
            lower |= 0x01;
            lower <<= neededBytes;

            ref var writeHead = ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSpan), pos);
            Unsafe.WriteUnaligned(ref writeHead, lower);

            // Write the 2 byte overflow unconditionally
            ushort upper = (ushort)(value >> (63 - neededBytes));
            writeHead = ref Unsafe.Add(ref writeHead, sizeof(ulong));
            Unsafe.WriteUnaligned(ref writeHead, upper);
        }
    }
}