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
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        private ReadOnlySequence<byte> input;
        // ReSharper restore FieldCanBeMadeReadOnly.Local

        private ReadOnlySpan<byte> currentSpan;
        private SequencePosition nextSequencePosition;
        private int bufferPos;
        private int bufferSize;
        private long previousBuffersSize;

        public Reader(ReadOnlySequence<byte> input, SerializerSession session)
        {
            this.input = input;
            this.Session = session;
            this.nextSequencePosition = input.Start;
            this.currentSpan = input.First.Span;
            this.bufferPos = 0;
            this.bufferSize = this.currentSpan.Length;
            this.previousBuffersSize = 0;
        }

        public SerializerSession Session { get; }

        public long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.previousBuffersSize + this.bufferPos;
        }

        public void Skip(long count)
        {
            var end = this.Position + count;
            while (this.Position < end)
            {
                if (this.Position + this.bufferSize >= end)
                {
                    this.bufferPos = (int)(end - this.previousBuffersSize);
                }
                else
                {
                    this.MoveNext();
                }
            }
        }

        /// <summary>
        /// Creates a new reader beginning at the specified position.
        /// </summary>
        public Reader ForkFrom(long position) => new Reader(this.input.Slice(position), this.Session);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void MoveNext()
        {
            this.previousBuffersSize += this.bufferSize;

            // If this is the first call to MoveNext then nextSequencePosition is invalid and must be moved to the second position.
            if (this.nextSequencePosition.Equals(this.input.Start)) this.input.TryGet(ref this.nextSequencePosition, out _);

            if (!this.input.TryGet(ref this.nextSequencePosition, out var memory))
            {
                this.currentSpan = memory.Span;
                ThrowInsufficientData();
            }

            this.currentSpan = memory.Span;
            this.bufferPos = 0;
            this.bufferSize = this.currentSpan.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            var pos = this.bufferPos;
            var span = this.currentSpan;
            if ((uint)pos >= (uint)span.Length) return this.ReadByteSlow();
            var result = span[pos];

            this.bufferPos = pos + 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private byte ReadByteSlow()
        {
            this.MoveNext();
            return this.currentSpan[this.bufferPos++];
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32() => (int)this.ReadUInt32();

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            const int width = 4;
            if (this.bufferPos + width > this.bufferSize) return ReadSlower(ref this);

            var result = BinaryPrimitives.ReadUInt32LittleEndian(this.currentSpan.Slice(this.bufferPos, width));
            this.bufferPos += width;
            return result;

            uint ReadSlower(ref Reader r)
            {
                uint b1 = r.ReadByte();
                uint b2 = r.ReadByte();
                uint b3 = r.ReadByte();
                uint b4 = r.ReadByte();

                return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64() => (long)this.ReadUInt64();

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            const int width = 8;
            if (this.bufferPos + width > this.bufferSize) return ReadSlower(ref this);

            var result = BinaryPrimitives.ReadUInt64LittleEndian(this.currentSpan.Slice(this.bufferPos, width));
            this.bufferPos += width;
            return result;

            ulong ReadSlower(ref Reader r)
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
        public float ReadFloat() => BitConverter.ToSingle(BitConverter.GetBytes(this.ReadInt32()), 0);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP
        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());
#else
        public double ReadDouble() => BitConverter.ToDouble(BitConverter.GetBytes(this.ReadInt64()), 0);
#endif

        public decimal ReadDecimal()
        {
            var parts = new[] { this.ReadInt32(), this.ReadInt32(), this.ReadInt32(), this.ReadInt32() };
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
            this.ReadBytes(in destination);
            return bytes;
        }

        public void ReadBytes(in Span<byte> destination)
        {
            if (this.bufferPos + destination.Length <= this.bufferSize)
            {
                this.currentSpan.Slice(this.bufferPos, destination.Length).CopyTo(destination);
                this.bufferPos += destination.Length;
                return;
            }

            CopySlower(in destination, ref this);

            void CopySlower(in Span<byte> d, ref Reader reader)
            {
                var dest = d;
                while (true)
                {
                    var writeSize = Math.Min(dest.Length, reader.currentSpan.Length - reader.bufferPos);
                    reader.currentSpan.Slice(reader.bufferPos, writeSize).CopyTo(dest);
                    reader.bufferPos += writeSize;
                    dest = dest.Slice(writeSize);

                    if (dest.Length == 0) break;

                    reader.MoveNext();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBytes(int length, out ReadOnlySpan<byte> bytes)
        {
            if (this.bufferPos + length <= this.bufferSize)
            {
                bytes = this.currentSpan.Slice(this.bufferPos, length);
                this.bufferPos += length;
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
            var pos = this.bufferPos;

            if (!BitConverter.IsLittleEndian || pos + 8 > this.currentSpan.Length)
            {
                return this.ReadVarUInt32Slow();
            }

            // The number of zeros in the msb position dictates the number of bytes to be read.
            // Up to a maximum of 5 for a 32bit integer.
            ref byte readHead = ref Unsafe.Add(ref MemoryMarshal.GetReference(this.currentSpan), pos);

            ulong result = Unsafe.ReadUnaligned<ulong>(ref readHead);
            var bytesNeeded = BitOperations.TrailingZeroCount(result) + 1;
            result >>= bytesNeeded;
            this.bufferPos += bytesNeeded;

            // Mask off invalid data
            var fullWidthReadMask = ~((ulong)bytesNeeded - 5 + 1);
            var mask = ((1UL << (bytesNeeded * 7)) - 1) | fullWidthReadMask;
            result &= mask; 

            return (uint)result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private uint ReadVarUInt32Slow()
        {
            var header = this.ReadByte();
            var numBytes = BitOperations.TrailingZeroCount(0x0100U | header) + 1;

            // Widen to a ulong for the 5-byte case
            ulong result = header;

            // Read additional bytes as needed
            if (numBytes == 2)
            {
                result |= ((ulong)this.ReadByte() << 8);
            }
            else if (numBytes == 3)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
            }
            else if (numBytes == 4)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result |= ((ulong)this.ReadByte() << 24);
            }
            else if (numBytes == 5)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result |= ((ulong)this.ReadByte() << 24);
                result |= ((ulong)this.ReadByte() << 32);
            }

            result >>= numBytes;
            return (uint)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadVarUInt64()
        {
            var pos = this.bufferPos;

            if (!BitConverter.IsLittleEndian || pos + 10 > this.currentSpan.Length)
            {
                return this.ReadVarUInt64Slow();
            }

            // The number of zeros in the msb position dictates the number of bytes to be read.
            // Up to a maximum of 5 for a 32bit integer.
            ref byte readHead = ref Unsafe.Add(ref MemoryMarshal.GetReference(this.currentSpan), pos);
            
            ulong result = Unsafe.ReadUnaligned<ulong>(ref readHead);

            var bytesNeeded = BitOperations.TrailingZeroCount(result) + 1;
            result >>= bytesNeeded;
            this.bufferPos += bytesNeeded;

            ushort upper = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref readHead, sizeof(ulong)));
            result |= (((ulong)upper) << (64 - bytesNeeded));

            // Mask off invalid data
            var fullWidthReadMask = ~((ulong)bytesNeeded - 10 + 1);
            var mask = ((1UL << (bytesNeeded * 7)) - 1) | fullWidthReadMask;
            result &= mask; 

            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ulong ReadVarUInt64Slow()
        {
            var header = this.ReadByte();
            var numBytes = BitOperations.TrailingZeroCount(0x0100U | header) + 1;

            // Widen to a ulong for the 5-byte case
            ulong result = header;

            // Read additional bytes as needed
            if (numBytes == 1)
            {
                result >>= 1;
                return result;
            }
            if (numBytes == 2)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result >>= 2;
                return result;
            }
            else if (numBytes == 3)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result >>= 3;
                return result;
            }
            else if (numBytes == 4)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result |= ((ulong)this.ReadByte() << 24);
                result >>= 4;
                return result;
            }
            else if (numBytes == 5)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result |= ((ulong)this.ReadByte() << 24);
                result |= ((ulong)this.ReadByte() << 32);
                result >>= 5;
                return result;
            }
            else if (numBytes == 6)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result |= ((ulong)this.ReadByte() << 24);
                result |= ((ulong)this.ReadByte() << 32);
                result |= ((ulong)this.ReadByte() << 40);
                result >>= 6;
                return result;
            }
            else if (numBytes == 7)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result |= ((ulong)this.ReadByte() << 24);
                result |= ((ulong)this.ReadByte() << 32);
                result |= ((ulong)this.ReadByte() << 40);
                result |= ((ulong)this.ReadByte() << 48);
                result >>= 7;
                return result;
            }
            else if (numBytes == 8)
            {
                result |= ((ulong)this.ReadByte() << 8);
                result |= ((ulong)this.ReadByte() << 16);
                result |= ((ulong)this.ReadByte() << 24);
                result |= ((ulong)this.ReadByte() << 32);

                result |= ((ulong)this.ReadByte() << 40);
                result |= ((ulong)this.ReadByte() << 48);
                result |= ((ulong)this.ReadByte() << 56);
                result >>= 8;
                return result;
            }
            else if (numBytes == 9)
            {
                result |= ((ulong)this.ReadByte() << 8);

                // If there was more than one byte worth of trailing zeros, read again now that we have more data.
                numBytes = BitOperations.TrailingZeroCount(result) + 1;

                if (numBytes == 9)
                {
                    result |= ((ulong)this.ReadByte() << 16);
                    result |= ((ulong)this.ReadByte() << 24);
                    result |= ((ulong)this.ReadByte() << 32);

                    result |= ((ulong)this.ReadByte() << 40);
                    result |= ((ulong)this.ReadByte() << 48);
                    result |= ((ulong)this.ReadByte() << 56);
                    result >>= 9;

                    var upper = (ushort)this.ReadByte();
                    result |= (((ulong)upper) << (64 - 9));
                    return result;
                }
                else if (numBytes == 10)
                {
                    result |= ((ulong)this.ReadByte() << 16);
                    result |= ((ulong)this.ReadByte() << 24);
                    result |= ((ulong)this.ReadByte() << 32);

                    result |= ((ulong)this.ReadByte() << 40);
                    result |= ((ulong)this.ReadByte() << 48);
                    result |= ((ulong)this.ReadByte() << 56);
                    result >>= 10;

                    var upper = (ushort)((ushort)this.ReadByte() | (ushort)((ushort)this.ReadByte() << 8));
                    result |= (((ulong)upper) << (64 - 10));
                    return result;
                }
            }

            return ExceptionHelper.ThrowArgumentOutOfRange<ulong>("value");
        }
    }
}