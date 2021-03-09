using BenchmarkDotNet.Attributes;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using Xunit;
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    public class MegaGraphBenchmark
    {
        private static readonly Serializer<Dictionary<string, int>> HagarSerializer;
        private static readonly byte[] HagarInput;
        private static readonly SerializerSession Session;
        private static readonly Dictionary<string, int> Value;

        static MegaGraphBenchmark()
        {
            const int Size = 250_000;
            Value = new Dictionary<string, int>(Size);
            var bumper = new string('x', 3000);
            for (var i = 0; i < Size; i++)
            {
                Value[i.ToString(CultureInfo.InvariantCulture) + bumper] = i;
            }
            
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<Dictionary<string, int>>>();
            Session = services.GetRequiredService<SerializerSessionPool>().GetSession();
            var pipe = new Pipe(new PipeOptions(readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline));
            var writer = pipe.Writer.CreateWriter(Session);
            HagarSerializer.Serialize(Value, ref writer);
            pipe.Writer.FlushAsync();
            pipe.Reader.TryRead(out var result);
            HagarInput = result.Buffer.ToArray();
        }

        [Fact]
        [Benchmark]
        public object Deserialize()
        {
            Session.FullReset();
            var instance = HagarSerializer.Deserialize(HagarInput, Session);
            return instance;
        }

        [Fact]
        [Benchmark]
        public int Serialize()
        {
            Session.FullReset();
            using (var buffer = new FakePooledBufferWriter(4096))
            {
                var writer = buffer.CreateWriter(Session);
                HagarSerializer.Serialize(Value, ref writer);
                return writer.Position;
            }
        }

        public class FakePooledBufferWriter : IBufferWriter<byte>, IDisposable
        {
            private readonly List<(byte[], int)> _committed = new();
            private readonly int _maxAllocationSize;
            private byte[] _current = Array.Empty<byte>();

            public FakePooledBufferWriter(int maxAllocationSize)
            {
                _maxAllocationSize = maxAllocationSize;
            }

            public void Advance(int bytes)
            {
                if (bytes == 0)
                {
                    return;
                }

                _committed.Add((_current, bytes));
                _current = Array.Empty<byte>();
            }

            public void Dispose()
            {
                foreach (var (array, _) in _committed)
                {
                    if (array.Length == 0)
                    {
                        continue;
                    }

                    ArrayPool<byte>.Shared.Return(array);
                }

                _committed.Clear();
            }

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                if (sizeHint == 0)
                {
                    sizeHint = _current.Length + 1;
                }

                if (sizeHint < _current.Length)
                {
                    throw new InvalidOperationException("Attempted to allocate a new buffer when the existing buffer has sufficient free space.");
                }

                var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Min(sizeHint, _maxAllocationSize));
                _current.CopyTo(newBuffer.AsSpan());
                _current = newBuffer;
                return _current;
            }

            public Span<byte> GetSpan(int sizeHint)
            {
                if (sizeHint == 0)
                {
                    sizeHint = _current.Length + 1;
                }

                if (sizeHint < _current.Length)
                {
                    throw new InvalidOperationException("Attempted to allocate a new buffer when the existing buffer has sufficient free space.");
                }

                var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Min(sizeHint, _maxAllocationSize));
                _current.CopyTo(newBuffer.AsSpan());
                _current = newBuffer;
                return _current;
            }
        }
    }
}