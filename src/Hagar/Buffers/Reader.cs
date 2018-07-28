using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Hagar.Buffers
{
    public sealed class Reader
    {
        private ReadOnlySequence<byte> input;
        private ReadOnlyMemory<byte> currentBuffer;
        private SequencePosition currentBufferStart;
        private int bufferPos;
        private int bufferSize;
        private long previousBuffersSize;

        public Reader(ReadOnlySequence<byte> input)
        {
            this.input = input;
            this.currentBuffer = input.First;
            this.currentBufferStart = input.Start;
            this.bufferSize = this.currentBuffer.Length;
        }

        public long CurrentPosition => this.previousBuffersSize + this.bufferPos;
        
        public ReadOnlySpan<byte> CurrentSpan => currentBuffer.Span;
        
        public void Skip(long count)
        {
            var end = this.CurrentPosition + count;
            while (this.CurrentPosition < end)
            {
                if (this.CurrentPosition + this.bufferSize >= end)
                {
                    this.bufferPos = (int) (end - this.previousBuffersSize);
                }
                else
                {
                    MoveNext(out _);
                }
            }
        }

        /// <summary>
        /// Creates a new reader begining at the specified position.
        /// </summary>
        public Reader ForkFrom(long position)
        {
            var result = new Reader(this.input);
            result.Skip(position);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveNext(out ReadOnlySpan<byte> currentSpan)
        {
            this.previousBuffersSize += this.bufferSize;
            
            if (!this.input.TryGet(ref this.currentBufferStart, out this.currentBuffer)) ThrowInsufficientData();

            currentSpan = this.CurrentSpan;
            this.bufferPos = 0;
            this.bufferSize = currentSpan.Length;
        }
        
        public byte ReadByte()
        {
            var currentSpan = this.CurrentSpan;
            return ReadByte(ref currentSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte(ref ReadOnlySpan<byte> currentSpan)
        {
            if (this.bufferPos == this.bufferSize) MoveNext(out currentSpan);
            return currentSpan[this.bufferPos++];
        }
        
        public int ReadInt32()
        {
            var currentSpan = this.CurrentSpan;
            return (int)ReadUInt32(ref currentSpan);
        }
        
        public uint ReadUInt32()
        {
            var currentSpan = this.CurrentSpan;
            return ReadUInt32(ref currentSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32(ref ReadOnlySpan<byte> currentSpan)
        {
            const int width = 4;
            if (this.bufferPos + width > this.bufferSize) return ReadSlower(ref currentSpan);

            var result = BinaryPrimitives.ReadUInt32LittleEndian(currentSpan.Slice(this.bufferPos, width));
            this.bufferPos += width;
            return result;
            
            uint ReadSlower(ref ReadOnlySpan<byte> c)
            {
                uint b1 = ReadByte(ref c);
                uint b2 = ReadByte(ref c);
                uint b3 = ReadByte(ref c);
                uint b4 = ReadByte(ref c);

                return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
            }
        }
        
        public ulong ReadUInt64()
        {
            var currentSpan = this.CurrentSpan;
            return ReadUInt64(ref currentSpan);
        }
        
        public long ReadInt64()
        {
            var currentSpan = this.CurrentSpan;
            return (long)ReadUInt64(ref currentSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64(ref ReadOnlySpan<byte> currentSpan)
        {
            const int width = 8;
            if (this.bufferPos + width > this.bufferSize) return ReadSlower(ref currentSpan);

            var result = BinaryPrimitives.ReadUInt64LittleEndian(currentSpan.Slice(this.bufferPos, width));
            this.bufferPos += width;
            return result;

            ulong ReadSlower(ref ReadOnlySpan<byte> c)
            {
                ulong b1 = ReadByte(ref c);
                ulong b2 = ReadByte(ref c);
                ulong b3 = ReadByte(ref c);
                ulong b4 = ReadByte(ref c);
                ulong b5 = ReadByte(ref c);
                ulong b6 = ReadByte(ref c);
                ulong b7 = ReadByte(ref c);
                ulong b8 = ReadByte(ref c);

                return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24)
                       | (b5 << 32) | (b6 << 40) | (b7 << 48) | (b8 << 56);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInsufficientData()
        {
            throw new InvalidOperationException("Insufficient data present in buffer.");
        }
        
#if NETCOREAPP2_1
        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt32());
#else
        public float ReadFloat() => BitConverter.ToSingle(BitConverter.GetBytes(ReadInt32()), 0);
#endif

        
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
        
        public byte[] ReadBytes(int count)
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

            CopySlower(destination);

            void CopySlower(Span<byte> dest)
            {
                var src = this.CurrentSpan;
                while (true)
                {
                    var writeSize = Math.Min(dest.Length, src.Length - this.bufferPos);
                    src.Slice(this.bufferPos, writeSize).CopyTo(dest);
                    this.bufferPos += writeSize;
                    dest = dest.Slice(writeSize);

                    if (dest.Length == 0) break;

                    MoveNext(out src);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBytes(int length, out ReadOnlySpan<byte> bytes)
        {
            if (this.bufferPos + length <= this.bufferSize)
            {
                bytes = CurrentSpan.Slice(this.bufferPos, length);
                this.bufferPos += length;
                return true;
            }

            bytes = default;
            return false;
        }
    }
}