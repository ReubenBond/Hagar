using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Hagar.Buffers
{
    public ref struct Reader
    {
        private ReadOnlySequence<byte> input;
        private ReadOnlySpan<byte> currentSpan;
        private SequencePosition currentBufferStart;
        private int bufferPos;
        private int bufferSize;
        private long previousBuffersSize;

        public Reader(ReadOnlySequence<byte> input)
        {
            this.input = input;
            this.currentSpan = input.First.Span;
            this.currentBufferStart = input.Start;
            this.bufferPos = 0;
            this.bufferSize = this.currentSpan.Length;
            this.previousBuffersSize = 0;
        }

        public long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.previousBuffersSize + this.bufferPos;
        }

        public ReadOnlySpan<byte> CurrentSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => currentSpan;
        }

        public void Skip(long count)
        {
            var end = this.Position + count;
            while (this.Position < end)
            {
                if (this.Position + this.bufferSize >= end)
                {
                    this.bufferPos = (int) (end - this.previousBuffersSize);
                }
                else
                {
                    MoveNext();
                }
            }
        }

        /// <summary>
        /// Creates a new reader begining at the specified position.
        /// </summary>
        public Reader ForkFrom(long position) => new Reader(this.input.Slice(position));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveNext()
        {
            this.previousBuffersSize += this.bufferSize;

            if (!this.input.TryGet(ref this.currentBufferStart, out var memory))
            {
                this.currentSpan = memory.Span;
                ThrowInsufficientData();
            }

            currentSpan = memory.Span;
            this.bufferPos = 0;
            this.bufferSize = currentSpan.Length;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (this.bufferPos == this.bufferSize) MoveNext();
            return currentSpan[this.bufferPos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            return (int)ReadUInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            const int width = 4;
            if (this.bufferPos + width > this.bufferSize) return ReadSlower(ref this);

            var result = BinaryPrimitives.ReadUInt32LittleEndian(currentSpan.Slice(this.bufferPos, width));
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            return (long)ReadUInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            const int width = 8;
            if (this.bufferPos + width > this.bufferSize) return ReadSlower(ref this);

            var result = BinaryPrimitives.ReadUInt64LittleEndian(currentSpan.Slice(this.bufferPos, width));
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
        private static void ThrowInsufficientData()
        {
            throw new InvalidOperationException("Insufficient data present in buffer.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP2_1
        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt32());
#else
        public float ReadFloat() => BitConverter.ToSingle(BitConverter.GetBytes(ReadInt32()), 0);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP2_1
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
            if (this.bufferPos + destination.Length <= this.bufferSize)
            {
                this.CurrentSpan.Slice(this.bufferPos, destination.Length).CopyTo(destination);
                this.bufferPos += destination.Length;
                return;
            }

            CopySlower(in destination, ref this);

            void CopySlower(in Span<byte> d, ref Reader reader)
            {
                var dest = d;
                while (true)
                {
                    var writeSize = Math.Min(d.Length, reader.currentSpan.Length - reader.bufferPos);
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
                bytes = currentSpan.Slice(this.bufferPos, length);
                this.bufferPos += length;
                return true;
            }

            bytes = default;
            return false;
        }
    }
}