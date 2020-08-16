using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Hagar.TestKit
{
    [ExcludeFromCodeCoverage]
    public static class BufferTestHelper<TValue>
    {
        public static IBufferTestSerializer[] GetTestSerializers(IServiceProvider serviceProvider) => new IBufferTestSerializer[]
            {
                ActivatorUtilities.CreateInstance<MultiSegmentBufferWriterTester>(serviceProvider, new MultiSegmentBufferWriterTester.Options { MaxAllocationSize = 17 }),
                ActivatorUtilities.CreateInstance<MultiSegmentBufferWriterTester>(serviceProvider, new MultiSegmentBufferWriterTester.Options { MaxAllocationSize = 128 }),
                ActivatorUtilities.CreateInstance<SingleSegmentBufferWriterTester>(serviceProvider),
                ActivatorUtilities.CreateInstance<StructBufferWriterTester>(serviceProvider),
            };

        public interface IBufferTestSerializer
        {
            IOutputBuffer Serialize(TValue input);
            void Deserialize(ReadOnlySequence<byte> buffer, out TValue output);
        }

        [ExcludeFromCodeCoverage]
        private abstract class BufferTester<TBufferWriter> : IBufferTestSerializer where TBufferWriter : IBufferWriter<byte>, IOutputBuffer
        {
            private readonly SessionPool _sessionPool;
            private readonly Serializer<TValue> _serializer;

            protected BufferTester(IServiceProvider serviceProvider)
            {
                _sessionPool = serviceProvider.GetRequiredService<SessionPool>();
                _serializer = serviceProvider.GetRequiredService<Serializer<TValue>>();
            }

            protected abstract TBufferWriter CreateBufferWriter();

            public IOutputBuffer Serialize(TValue input)
            {
                using var session = _sessionPool.GetSession();
                var writer = new Writer<TBufferWriter>(CreateBufferWriter(), session);
                _serializer.Serialize(ref writer, input);
                return writer.Output;
            }

            public void Deserialize(ReadOnlySequence<byte> buffer, out TValue output)
            {
                using var session = _sessionPool.GetSession();
                var reader = new Reader(buffer, session);
                output = _serializer.Deserialize(ref reader);
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        [ExcludeFromCodeCoverage]
        private class MultiSegmentBufferWriterTester : BufferTester<TestMultiSegmentBufferWriter>
        {
            private readonly Options _options;

            public MultiSegmentBufferWriterTester(IServiceProvider serviceProvider, Options options) : base(serviceProvider)
            {
                this._options = options;
            }

            public class Options
            {
                public int MaxAllocationSize { get; set; }
            }

            protected override TestMultiSegmentBufferWriter CreateBufferWriter() => new TestMultiSegmentBufferWriter(_options.MaxAllocationSize);

            public override string ToString() => $"{nameof(TestMultiSegmentBufferWriter)} {nameof(_options.MaxAllocationSize)}: {_options.MaxAllocationSize}";
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        [ExcludeFromCodeCoverage]
        private class SingleSegmentBufferWriterTester : BufferTester<TestSingleSegmentBufferWriter>
        {
            protected override TestSingleSegmentBufferWriter CreateBufferWriter() => new TestSingleSegmentBufferWriter(new byte[102400]);

            public SingleSegmentBufferWriterTester(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override string ToString() => $"{nameof(TestSingleSegmentBufferWriter)}";
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        [ExcludeFromCodeCoverage]
        private class StructBufferWriterTester : BufferTester<TestBufferWriterStruct>
        {
            protected override TestBufferWriterStruct CreateBufferWriter() => new TestBufferWriterStruct(new byte[102400]);

            public StructBufferWriterTester(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override string ToString() => $"{nameof(TestBufferWriterStruct)}";
        }
    }
}