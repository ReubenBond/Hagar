using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hagar.TestKit
{
    [Trait("Category", "BVT")]
    public abstract class FieldCodecTester<TValue, TCodec> where TCodec : class, IFieldCodec<TValue>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly SessionPool sessionPool;
        
        protected FieldCodecTester()
        {
            var services = new ServiceCollection();
            services.AddHagar(hagar => hagar.FieldCodecs.Add(typeof(TCodec)));
            services.AddSingleton<TCodec>();

            // ReSharper disable once VirtualMemberCallInConstructor
            this.ConfigureServices(services);

            this.serviceProvider = services.BuildServiceProvider();
            this.sessionPool = this.serviceProvider.GetService<SessionPool>();
        }

        private int[] MaxSegmentSizes => new[] { 0, 1, 4, 16 };

        protected virtual void ConfigureServices(IServiceCollection services)
        {
        }

        protected virtual TCodec CreateCodec() => this.serviceProvider.GetRequiredService<TCodec>();
        protected abstract TValue CreateValue();
        protected abstract TValue[] TestValues { get; }
        protected virtual bool Equals(TValue left, TValue right) => EqualityComparer<TValue>.Default.Equals(left, right);
        
        [Fact]
        public void CorrectlyAdvancesReferenceCounter()
        {
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, this.sessionPool.GetSession());
            var writerCodec = this.CreateCodec();
            var beforeReference = writer.Session.ReferencedObjects.CurrentReferenceId;

            // Write the field. This should involve marking at least one reference in the session.
            Assert.Equal(0, writer.Position);

            writerCodec.WriteField(ref writer, 0, typeof(TValue), this.CreateValue());
            Assert.True(writer.Position > 0);
            
            writer.Commit();
            var afterReference = writer.Session.ReferencedObjects.CurrentReferenceId;
            Assert.True(beforeReference < afterReference, $"Writing a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();

            pipe.Reader.TryRead(out var readResult);
            var reader = new Reader(readResult.Buffer, this.sessionPool.GetSession());

            var previousPos = reader.Position;
            Assert.Equal(0, previousPos);
            var readerCodec = this.CreateCodec();
            var readField = reader.ReadFieldHeader();

            Assert.True(reader.Position > previousPos);
            previousPos = reader.Position;

            beforeReference = reader.Session.ReferencedObjects.CurrentReferenceId;
            readerCodec.ReadValue(ref reader, readField);

            Assert.True(reader.Position > previousPos);

            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
            afterReference = reader.Session.ReferencedObjects.CurrentReferenceId;
            Assert.True(beforeReference < afterReference, $"Reading a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
        }

        [Fact]
        public void CanRoundTripViaSerializer()
        {
            var serializer = this.serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in this.TestValues)
            {
                var buffer = new TestSingleSegmentBufferWriter(new byte[10240]);
                
                var writer = new Writer<TestSingleSegmentBufferWriter>(buffer, this.sessionPool.GetSession());
                serializer.Serialize(ref writer, original);

                var reader = new Reader(buffer.GetReadOnlySequence(0), this.sessionPool.GetSession());
                var deserialized = serializer.Deserialize(ref reader);

                Assert.True(this.Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
            }
        }

        [Fact]
        public void RoundTrippedValuesEqual()
        {
            this.TestRoundTrippedValue(this.CreateValue());
        }

        [Fact]
        public void CanRoundTripDefaultValueViaCodec()
        {
            this.TestRoundTrippedValue(default);
        }

        [Fact]
        public void CanSkipValue()
        {
            this.CanBeSkipped(default);
        }

        [Fact]
        public void CanSkipDefaultValue()
        {
            this.CanBeSkipped(default);
        }

        [Fact]
        public void CorrectlyHandlesBuffers()
        {
            var testers = BufferTestHelper<TValue>.GetTestSerializers(this.serviceProvider);

            foreach (var tester in testers)
            {
                foreach (var maxSegmentSize in this.MaxSegmentSizes)
                foreach (var value in this.TestValues)
                {
                    var buffer = tester.Serialize(value);
                    var sequence = buffer.GetReadOnlySequence(maxSegmentSize);
                    tester.Deserialize(sequence, out var output);
                    var bufferWriterType = tester.GetType().BaseType?.GenericTypeArguments[1];
                    Assert.True(this.Equals(value, output),
                        $"Deserialized value {output} must be equal to serialized value {value}. " +
                        $"IBufferWriter<> type: {bufferWriterType}, Max Read Segment Size: {maxSegmentSize}. " +
                        $"Buffer: 0x{string.Join(" ", sequence.ToArray().Select(b => $"{b:X2}"))}");
                }
            }
        }

        private void CanBeSkipped(TValue original)
        {
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, this.sessionPool.GetSession());
            var writerCodec = this.CreateCodec();
            writerCodec.WriteField(ref writer, 0, typeof(TValue), original);
            var expectedLength = writer.Position;
            writer.Commit();
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();

            pipe.Reader.TryRead(out var readResult);
            
            {
                var reader = new Reader(readResult.Buffer, this.sessionPool.GetSession());
                var readField = reader.ReadFieldHeader();
                reader.SkipField(readField);
                Assert.Equal(expectedLength, reader.Position);
            }

            {
                var codec = new SkipFieldCodec();
                var reader = new Reader(readResult.Buffer, this.sessionPool.GetSession());
                var readField = reader.ReadFieldHeader();
                var shouldBeNull = codec.ReadValue(ref reader, readField);
                Assert.Null(shouldBeNull);
                Assert.Equal(expectedLength, reader.Position);
            }

            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
        }

        private void TestRoundTrippedValue(TValue original)
        {
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, this.sessionPool.GetSession());
            var writerCodec = this.CreateCodec();
            writerCodec.WriteField(ref writer, 0, typeof(TValue), original);
            writer.Commit();
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();

            pipe.Reader.TryRead(out var readResult);
            var reader = new Reader(readResult.Buffer, this.sessionPool.GetSession());
            var readerCodec = this.CreateCodec();
            var readField = reader.ReadFieldHeader();
            var deserialized = readerCodec.ReadValue(ref reader, readField);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
            Assert.True(this.Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
        }
    }
}
