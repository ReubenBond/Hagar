﻿using System;
using System.Buffers;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Hagar.Buffers.Adaptors
{
    /// <summary>
    /// A special-purpose <see cref="IBufferWriter{byte}"/> implementation for supporting <see cref="Span{Byte}"/> in <see cref="Writer{TBufferWriter}"/>.
    /// </summary>
    public struct SpanBufferWriter : IBufferWriter<byte>
    {
        private int _maxLength;
        private int _bytesWritten;

        internal SpanBufferWriter(Span<byte> buffer)
        {
            _maxLength = buffer.Length;
            _bytesWritten = 0;
        }

        public int BytesWritten => _bytesWritten;

        /// <inheritdoc />
        public void Advance(int count)
        {
            if (_bytesWritten + count > _maxLength)
            {
                ThrowInvalidCount();
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void ThrowInvalidCount() => throw new InvalidOperationException("Cannot advance past the end of the buffer");
            }

            _bytesWritten += count;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_bytesWritten + sizeHint > _maxLength)
            {
                ThrowInsufficientCapacity(sizeHint);
            }

            throw new NotSupportedException("Method is not supported on this instance");
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (_bytesWritten + sizeHint > _maxLength)
            {
                ThrowInsufficientCapacity(sizeHint);
            }

            throw new NotSupportedException("Method is not supported on this instance");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowInsufficientCapacity(int sizeHint) => throw new InvalidOperationException($"Insufficient capacity to perform the requested operation. Buffer size is {_maxLength}. Current length is {_bytesWritten} and requested size increase is {sizeHint}");
    }
}
