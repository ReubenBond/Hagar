using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Hagar.Session;

namespace Hagar.Buffers
{
    public ref struct Reader
    {
        private ReadOnlySequence<byte> input;
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
                    this.bufferPos = (int) (end - this.previousBuffersSize);
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
        public Reader ForkFrom(long position) => new Reader(this.input.Slice(position), this.Session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (this.bufferPos == this.bufferSize) MoveNext();
            return this.currentSpan[this.bufferPos++];
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

        public uint ReadVarUInt32()
        {
            if (this.bufferPos + 5 > this.bufferSize)
            {
                return ReadVarUInt32Slow();
            }

            int tmp = this.currentSpan[this.bufferPos++];
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = this.currentSpan[this.bufferPos++]) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            // Note that this has to use ReadRawByte() as we only ensure we've
                            // got at least 5 bytes at the start of the method. This lets us
                            // use the fast path in more cases, and we rarely hit this section of code.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadByte() < 128)
                                {
                                    return (uint)result;
                                }
                            }

                            ThrowInsufficientData();
                        }
                    }
                }
            }
            return (uint)result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private uint ReadVarUInt32Slow()
        {
            int tmp = ReadByte();
            if (tmp < 128)
            {
                return (uint)tmp;
            }
            int result = tmp & 0x7f;
            if ((tmp = ReadByte()) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = ReadByte()) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = ReadByte()) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        result |= (tmp = ReadByte()) << 28;
                        if (tmp >= 128)
                        {
                            // Discard upper 32 bits.
                            for (int i = 0; i < 5; i++)
                            {
                                if (ReadByte() < 128)
                                {
                                    return (uint)result;
                                }
                            }

                            ThrowInsufficientData();
                        }
                    }
                }
            }
            return (uint)result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadVarUInt64()
        {
            if (this.bufferPos + 10 > this.bufferSize)
            {
                return ReadVarUInt64Slow();
            }

            long tmp = this.currentSpan[this.bufferPos++];
            if (tmp < 128)
            {
                return (ulong)tmp;
            }
            long result = tmp & 0x7f;
            if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
            {
                result |= tmp << 7;
            }
            else
            {
                result |= (tmp & 0x7f) << 7;
                if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                {
                    result |= tmp << 14;
                }
                else
                {
                    result |= (tmp & 0x7f) << 14;
                    if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                    {
                        result |= tmp << 21;
                    }
                    else
                    {
                        result |= (tmp & 0x7f) << 21;
                        if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                        {
                            result |= tmp << 28;
                        }
                        else
                        {
                            result |= (tmp & 0x7f) << 28;
                            if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                            {
                                result |= tmp << 35;
                            }
                            else
                            {
                                result |= (tmp & 0x7f) << 35;
                                if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                                {
                                    result |= tmp << 42;
                                }
                                else
                                {
                                    result |= (tmp & 0x7f) << 42;
                                    if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                                    {
                                        result |= tmp << 49;
                                    }
                                    else
                                    {
                                        result |= (tmp & 0x7f) << 49;
                                        if ((tmp = this.currentSpan[this.bufferPos++]) < 128)
                                        {
                                            result |= tmp << 56;
                                        }
                                        else
                                        {
                                            result |= (tmp & 0x7f) << 56;
                                            result |= (tmp = this.currentSpan[this.bufferPos++]) << 63;
                                            if (tmp >= 128)
                                            {
                                                ThrowInsufficientData();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (ulong)result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ulong ReadVarUInt64Slow()
        {
            //TODO: Implement fast path
            int shift = 0;
            ulong result = 0;
            while (shift < 64)
            {
                byte b = ReadByte();
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
                shift += 7;
            }

            ThrowInsufficientData();
            return 0;
        }
    }
}