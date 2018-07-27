using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Hagar.Utilities;

namespace Hagar.Buffers
{
    public class Reader
    {
        private readonly ReadOnlySequence<byte> input;
        private SequencePosition position;
        
        private static readonly byte[] EmptyByteArray = new byte[0];
        
        public Reader(ReadOnlySequence<byte> input) : this(input, input.Start)
        {
        }

        public Reader(ReadOnlySequence<byte> input, SequencePosition position)
        {
            this.input = input;
            this.position = position;
        }
        
        public SequencePosition CurrentPosition => this.position;
        
        public long Length => this.input.Length;
        
        public byte ReadByte()
        {
            var readOnly = this.TryReadSpan(1);
            if (readOnly.Length >= 1)
            {
                return readOnly[0];
            }

            ThrowInsufficientData();
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int length)
        {
            this.position = this.input.GetPosition(length, this.position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(SequencePosition newPosition)
        {
            this.position = newPosition;
        }

        public Reader Copy() => new Reader(this.input, this.position);

        public void Reset() => this.position = this.input.Start;

        public int ReadInt()
        {
            const int width = 4;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return BinaryPrimitives.ReadInt32LittleEndian(readOnly);
            }

            return ReadSlower();

            int ReadSlower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return BinaryPrimitives.ReadInt32LittleEndian(span);
            }
        }

        public ReadOnlySpan<byte> TryReadSpan(int length)
        {
            if (!this.input.TryGet(ref this.position, out var mem, advance: false))
            {
                ThrowInsufficientData();
            }

            if (mem.Length < length) return default;
            
            // Return a span which is probably longer than requested.
            Advance(length);
            return mem.Span;
        }

        public void ReadSpan(in Span<byte> span)
        {
            var indexInSpan = 0;
            while (this.input.TryGet(ref this.position, out var mem, advance: false))
            {
                var memSpan = mem.Span;
                memSpan.CopyTo(span.Slice(indexInSpan));
                indexInSpan += memSpan.Length;
                Advance(memSpan.Length);
                if (indexInSpan == span.Length) return;
            }

            ThrowInsufficientData();
        }

        private static void ThrowInsufficientData()
        {
            throw new InvalidOperationException("Insufficient data present in buffer.");
        }

        public uint ReadUInt()
        {
            const int width = 4;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(readOnly);
            }

            return ReadSlower();
            
            uint ReadSlower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return BinaryPrimitives.ReadUInt32LittleEndian(span);
            }
        }

        public short ReadShort()
        {
            const int width = 2;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return BinaryPrimitives.ReadInt16LittleEndian(readOnly);
            }

            return ReadInt16Slower();

            short ReadInt16Slower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return BinaryPrimitives.ReadInt16LittleEndian(span);
            }
        }

        public ushort ReadUShort()
        {
            const int width = 2;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(readOnly);
            }

            return ReadSlower();

            ushort ReadSlower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return BinaryPrimitives.ReadUInt16LittleEndian(span);
            }
        }

        public long ReadLong()
        {
            const int width = 8;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return BinaryPrimitives.ReadInt64LittleEndian(readOnly);
            }

            return ReadSlower();

            long ReadSlower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return BinaryPrimitives.ReadInt64LittleEndian(span);
            }
        }

        public ulong ReadULong()
        {
            const int width = 8;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return BinaryPrimitives.ReadUInt64LittleEndian(readOnly);
            }

            return ReadSlower();

            ulong ReadSlower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return BinaryPrimitives.ReadUInt64LittleEndian(span);
            }
        }

        public float ReadFloat()
        {
            const int width = 4;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return MemoryMarshal.Read<float>(readOnly);
            }

            return ReadSlower();

            float ReadSlower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return MemoryMarshal.Read<float>(span);
            }
        }

        public double ReadDouble()
        {
            const int width = 4;
            var readOnly = TryReadSpan(width);
            if (readOnly.Length >= width)
            {
                return MemoryMarshal.Read<double>(readOnly);
            }

            return ReadSlower();

            double ReadSlower()
            {
                Span<byte> span = stackalloc byte[width];
                this.ReadSpan(in span);
                return MemoryMarshal.Read<double>(span);
            }
        }

        public decimal ReadDecimal()
        {
            var parts = new[] { ReadInt(), ReadInt(), ReadInt(), ReadInt() };
            return new decimal(parts);
        }

        public DateTime ReadDateTime()
        {
            var n = this.ReadLong();
            return n == 0 ? default : DateTime.FromBinary(n);
        }

        public string ReadString()
        {
            var n = this.ReadVarInt32();
            if (n == 0)
            {
                return string.Empty;
            }

            string s = null;
            // a length of -1 indicates that the string is null.
            if (-1 != n)
            {
                var readOnly = this.TryReadSpan(n);
                if (readOnly.Length >= n)
                {
#if NETCOREAPP2_1
                    s = Encoding.UTF8.GetString(readOnly.Slice(0, n));
#else
                    s = Encoding.UTF8.GetString(readOnly.Slice(0, n).ToArray(), 0, n);
#endif
                }
                else
                {
                    var bytes = ArrayPool<byte>.Shared.Rent(n);
                    var span = new Span<byte>(bytes, 0, n);
                    this.ReadSpan(span);
#if NETCOREAPP2_1
                    s = Encoding.UTF8.GetString(span.Slice(0, n));
#else
                    s = Encoding.UTF8.GetString(span.Slice(0, n).ToArray(), 0, n);
#endif
                    ArrayPool<byte>.Shared.Return(bytes);
                }
            }

            return s;
        }

        public byte[] ReadBytes(int count)
        {
            if (count == 0)
            {
                return EmptyByteArray;
            }

            var bytes = new byte[count];
            var readOnly = this.TryReadSpan(count);
            if (readOnly.Length >= count)
            {
                readOnly.Slice(0, count).CopyTo(bytes);
            }
            else
            {
                this.ReadSpan(bytes);
            }

            return bytes;
        }
    }
}