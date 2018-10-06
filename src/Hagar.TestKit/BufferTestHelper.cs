using System;
using System.Buffers;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;

namespace Hagar.TestKit
{
    public static class BufferTestHelper<TValue>
    {
        public static IBufferTestSerializer[] GetTestSerializers(IServiceProvider serviceProvider)
        {
            return new IBufferTestSerializer[]
            {
                ActivatorUtilities.CreateInstance<MultiSegmentBufferWriterTester>(serviceProvider, new MultiSegmentBufferWriterTester.Options { MaxAllocationSize = 17 }),
                ActivatorUtilities.CreateInstance<MultiSegmentBufferWriterTester>(serviceProvider, new MultiSegmentBufferWriterTester.Options { MaxAllocationSize = 128 }),
                ActivatorUtilities.CreateInstance<SingleSegmentBufferWriterTester>(serviceProvider),
                ActivatorUtilities.CreateInstance<StructBufferWriterTester>(serviceProvider),
            };
        }

        public interface IBufferTestSerializer
        {
            IOutputBuffer Serialize(TValue input);
            void Deserialize(ReadOnlySequence<byte> buffer, out TValue output);
        }

        private abstract class BufferTester<TBufferWriter> : IBufferTestSerializer where TBufferWriter : IBufferWriter<byte>, IOutputBuffer
        {
            private readonly SessionPool sessionPool;
            private readonly Serializer<TValue> serializer;

            protected BufferTester(IServiceProvider serviceProvider)
            {
                this.sessionPool = serviceProvider.GetRequiredService<SessionPool>();
                this.serializer = serviceProvider.GetRequiredService<Serializer<TValue>>();
            }

            protected abstract TBufferWriter CreateBufferWriter();

            public IOutputBuffer Serialize(TValue input)
            {
                using (var session = this.sessionPool.GetSession())
                {
                    var writer = new Writer<TBufferWriter>(this.CreateBufferWriter(), session);
                    this.serializer.Serialize(ref writer, input);
                    return writer.Output;
                }
            }

            public void Deserialize(ReadOnlySequence<byte> buffer, out TValue output)
            {
                using (var session = this.sessionPool.GetSession())
                {
                    var reader = new Reader(buffer, session);
                    output = this.serializer.Deserialize(ref reader);
                }
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class MultiSegmentBufferWriterTester : BufferTester<TestMultiSegmentBufferWriter>
        {
            private readonly Options options;

            public MultiSegmentBufferWriterTester(IServiceProvider serviceProvider, Options options) : base(serviceProvider)
            {
                this.options = options;
            }

            public class Options
            {
                public int MaxAllocationSize { get; set; }
            }

            protected override TestMultiSegmentBufferWriter CreateBufferWriter() => new TestMultiSegmentBufferWriter(this.options.MaxAllocationSize);

            public override string ToString() => $"{nameof(TestMultiSegmentBufferWriter)} {nameof(this.options.MaxAllocationSize)}: {this.options.MaxAllocationSize}";
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class SingleSegmentBufferWriterTester : BufferTester<TestSingleSegmentBufferWriter>
        {
            protected override TestSingleSegmentBufferWriter CreateBufferWriter() => new TestSingleSegmentBufferWriter(new byte[102400]);

            public SingleSegmentBufferWriterTester(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override string ToString() => $"{nameof(TestSingleSegmentBufferWriter)}";
        }

        // ReSharper disable once ClassNeverInstantiated.Local
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