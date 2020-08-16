using System;
using System.Buffers;
using System.Buffers.Binary;
#if NETCOREAPP
using System.Numerics;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hagar.Session;
using Hagar.Utilities;

namespace Hagar.Buffers
{
    public ref struct Reader
    {
#pragma warning disable IDE0044 // Add readonly modifier
        private ReadOnlySequence<byte> _input;
#pragma warning restore IDE0044 // Add readonly modifier

        private ReadOnlySpan<byte> _currentSpan;
        private SequencePosition _nextSequencePosition;
        private int _bufferPos;
        private int _bufferSize;
        private long _previousBuffersSize;

        public Reader(ReadOnlySequence<byte> input, SerializerSession session)
        {
            _input = input;
            Session = session;
            _nextSequencePosition = input.Start;
            _currentSpan = input.First.Span;
            _bufferPos = 0;
            _bufferSize = _currentSpan.Length;
            _previousBuffersSize = 0;
        }

        public SerializerSession Session { get; }

        public long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _previousBuffersSize + _bufferPos;
        }

        public void Skip(long count)
        {
            var end = Position + count;
            while (Position < end)
            {
                if (Position + _bufferSize >= end)
                {
                    _bufferPos = (int)(end - _previousBuffersSize);
                }
                else
                {
                    MoveNext();
                }
            }
        }

        /// <summary>
        /// Creates a new reader beginning at the specified position.
        /// </summary>
        public Reader ForkFrom(long position) => new Reader(_input.Slice(position), Session);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MoveNext()
        {
            _previousBuffersSize += _bufferSize;

            // If this is the first call to MoveNext then nextSequencePosition is invalid and must be moved to the second position.
            if (_nextSequencePosition.Equals(_input.Start))
            {
                _ = _input.TryGet(ref _nextSequencePosition, out _);
            }

            if (!_input.TryGet(ref _nextSequencePosition, out var memory))
            {
                _currentSpan = memory.Span;
                ThrowInsufficientData();
            }

            _currentSpan = memory.Span;
            _bufferPos = 0;
            _bufferSize = _currentSpan.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            var pos = _bufferPos;
            var span = _currentSpan;
            if ((uint)pos >= (uint)span.Length)
            {
                return ReadByteSlow();
            }

            var result = span[pos];

            _bufferPos = pos + 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private byte ReadByteSlow()
        {
            MoveNext();
            return _currentSpan[_bufferPos++];
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32() => (int)ReadUInt32();

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            const int width = 4;
            if (_bufferPos + width > _bufferSize)
            {
                return ReadSlower(ref this);
            }

            var result = BinaryPrimitives.ReadUInt32LittleEndian(_currentSpan.Slice(_bufferPos, width));
            _bufferPos += width;
            return result;

            static uint ReadSlower(ref Reader r)
            {
                uint b1 = r.ReadByte();
                uint b2 = r.ReadByte();
                uint b3 = r.ReadByte();
                uint b4 = r.ReadByte();

                return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64() => (long)ReadUInt64();

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            const int width = 8;
            if (_bufferPos + width > _bufferSize)
            {
                return ReadSlower(ref this);
            }

            var result = BinaryPrimitives.ReadUInt64LittleEndian(_currentSpan.Slice(_bufferPos, width));
            _bufferPos += width;
            return result;

            static ulong ReadSlower(ref Reader r)
            {
                ulong b1 = r.ReadByte();
                ulong b2 = r.ReadByte();
                ulong b3 = r.ReadByte();
                ulong b4 = r.ReadByte();
                ulong b5 = r.ReadByte();
                ulong b6 = r.ReadByte();
                ulong b7 = r.ReadByte();
                ulong b8 = r.ReadByte();

                return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24)
                       | (b5 << 32) | (b6 << 40) | (b7 << 48) | (b8 << 56);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInsufficientData() => throw new InvalidOperationException("Insufficient data present in buffer.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP
        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt32());
#else
        public float ReadFloat() => BitConverter.ToSingle(BitConverter.GetBytes(ReadInt32()), 0);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP
        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());
#else
        public double ReadDouble() => BitConverter.ToDouble(BitConverter.GetBytes(ReadInt64()), 0);
#endif

        public decimal ReadDecimal()
        {
            var parts = new[] { ReadInt32(), ReadInt32(), ReadInt32(), ReadInt32() };
            return new decimal(parts);
        }

        public byte[] ReadBytes(uint count)
        {
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            var bytes = new byte[count];
            var destination = new Span<byte>(bytes);
            ReadBytes(in destination);
            return bytes;
        }

        public void ReadBytes(in Span<byte> destination)
        {
            if (_bufferPos + destination.Length <= _bufferSize)
            {
                _currentSpan.Slice(_bufferPos, destination.Length).CopyTo(destination);
                _bufferPos += destination.Length;
                return;
            }

            CopySlower(in destination, ref this);

            static void CopySlower(in Span<byte> d, ref Reader reader)
            {
                var dest = d;
                while (true)
                {
                    var writeSize = Math.Min(dest.Length, reader._currentSpan.Length - reader._bufferPos);
                    reader._currentSpan.Slice(reader._bufferPos, writeSize).CopyTo(dest);
                    reader._bufferPos += writeSize;
                    dest = dest.Slice(writeSize);

                    if (dest.Length == 0)
                    {
                        break;
                    }

                    reader.MoveNext();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBytes(int length, out ReadOnlySpan<byte> bytes)
        {
            if (_bufferPos + length <= _bufferSize)
            {
                bytes = _currentSpan.Slice(_bufferPos, length);
                _bufferPos += length;
                return true;
            }

            bytes = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal uint ReadVarUInt32NoInlining() => ReadVarUInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe uint ReadVarUInt32()
        {
            var pos = _bufferPos;

            if (!BitConverter.IsLittleEndian || pos + 8 > _currentSpan.Length)
            {
                return ReadVarUInt32Slow();
            }

            // The number of zeros in the msb position dictates the number of bytes to be read.
            // Up to a maximum of 5 for a 32bit integer.
            ref byte readHead = ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSpan), pos);

            ulong result = Unsafe.ReadUnaligned<ulong>(ref readHead);
            var bytesNeeded = BitOperations.TrailingZeroCount(result) + 1;
            result >>= bytesNeeded;
            _bufferPos += bytesNeeded;

            // Mask off invalid data
            var fullWidthReadMask = ~((ulong)bytesNeeded - 5 + 1);
            var mask = ((1UL << (bytesNeeded * 7)) - 1) | fullWidthReadMask;
            result &= mask;

            return (uint)result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private uint ReadVarUInt32Slow()
        {
            var header = ReadByte();
            var numBytes = BitOperations.TrailingZeroCount(0x0100U | header) + 1;

            // Widen to a ulong for the 5-byte case
            ulong result = header;

            // Read additional bytes as needed
            var shiftBy = 8;
            var i = numBytes;
            while (--i > 0)
            {
                result |= (ulong)ReadByte() << shiftBy;
                shiftBy += 8;
            }

            result >>= numBytes;
            return (uint)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadVarUInt64()
        {
            var pos = _bufferPos;

            if (!BitConverter.IsLittleEndian || pos + 10 > _currentSpan.Length)
            {
                return ReadVarUInt64Slow();
            }

            // The number of zeros in the msb position dictates the number of bytes to be read.
            // Up to a maximum of 5 for a 32bit integer.
            ref byte readHead = ref Unsafe.Add(ref MemoryMarshal.GetReference(_currentSpan), pos);

            ulong result = Unsafe.ReadUnaligned<ulong>(ref readHead);

            var bytesNeeded = BitOperations.TrailingZeroCount(result) + 1;
            result >>= bytesNeeded;
            _bufferPos += bytesNeeded;

            ushort upper = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref readHead, sizeof(ulong)));
            result |= ((ulong)upper) << (64 - bytesNeeded);

            // Mask off invalid data
            var fullWidthReadMask = ~((ulong)bytesNeeded - 10 + 1);
            var mask = ((1UL << (bytesNeeded * 7)) - 1) | fullWidthReadMask;
            result &= mask;

            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ulong ReadVarUInt64Slow()
        {
            var header = ReadByte();
            var numBytes = BitOperations.TrailingZeroCount(0x0100U | header) + 1;

            // Widen to a ulong for the 5-byte case
            ulong result = header;

            // Read additional bytes as needed
            if (numBytes < 9)
            {
                var shiftBy = 8;
                var i = numBytes;
                while (--i > 0)
                {
                    result |= (ulong)ReadByte() << shiftBy;
                    shiftBy += 8;
                }

                result >>= numBytes;
                return result;
            }
            else
            {
                result |= (ulong)ReadByte() << 8;

                // If there was more than one byte worth of trailing zeros, read again now that we have more data.
                numBytes = BitOperations.TrailingZeroCount(result) + 1;

                if (numBytes == 9)
                {
                    result |= (ulong)ReadByte() << 16;
                    result |= (ulong)ReadByte() << 24;
                    result |= (ulong)ReadByte() << 32;

                    result |= (ulong)ReadByte() << 40;
                    result |= (ulong)ReadByte() << 48;
                    result |= (ulong)ReadByte() << 56;
                    result >>= 9;

                    var upper = (ushort)ReadByte();
                    result |= ((ulong)upper) << (64 - 9);
                    return result;
                }
                else if (numBytes == 10)
                {
                    result |= (ulong)ReadByte() << 16;
                    result |= (ulong)ReadByte() << 24;
                    result |= (ulong)ReadByte() << 32;

                    result |= (ulong)ReadByte() << 40;
                    result |= (ulong)ReadByte() << 48;
                    result |= (ulong)ReadByte() << 56;
                    result >>= 10;

                    var upper = (ushort)(ReadByte() | (ushort)(ReadByte() << 8));
                    result |= ((ulong)upper) << (64 - 10);
                    return result;
                }
            }

            return ExceptionHelper.ThrowArgumentOutOfRange<ulong>("value");
        }
    }
}