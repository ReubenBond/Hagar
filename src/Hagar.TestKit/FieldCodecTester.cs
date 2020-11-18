using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using Xunit;

namespace Hagar.TestKit
{
    [Trait("Category", "BVT")]
    [ExcludeFromCodeCoverage]
    public abstract class FieldCodecTester<TValue, TCodec> where TCodec : class, IFieldCodec<TValue>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SerializerSessionPool _sessionPool;

        protected FieldCodecTester()
        {
            var services = new ServiceCollection();
            _ = services.AddHagar(hagar => hagar.Configure(config => config.FieldCodecs.Add(typeof(TCodec))));

            if (!typeof(TCodec).IsAbstract && !typeof(TCodec).IsInterface)
            {
                _ = services.AddSingleton<TCodec>();
            }

            // ReSharper disable once VirtualMemberCallInConstructor
            _ = services.AddHagar(Configure);

            _serviceProvider = services.BuildServiceProvider();
            _sessionPool = _serviceProvider.GetService<SerializerSessionPool>();
        }

        protected IServiceProvider ServiceProvider => _serviceProvider;

        private static int[] MaxSegmentSizes => new[] { /*0, 1, 4,*/ 16 };

        protected virtual void Configure(IHagarBuilder builder)
        {
        }

        protected virtual TCodec CreateCodec() => _serviceProvider.GetRequiredService<TCodec>();
        protected abstract TValue CreateValue();
        protected abstract TValue[] TestValues { get; }
        protected virtual bool Equals(TValue left, TValue right) => EqualityComparer<TValue>.Default.Equals(left, right);

        [Fact]
        public void CorrectlyAdvancesReferenceCounterStream()
        {
            var stream = new MemoryStream();
            using var writerSession = _sessionPool.GetSession();
            using var readerSession = _sessionPool.GetSession();
            var writer = Writer.Create(stream, writerSession);
            var writerCodec = CreateCodec();

            // Write the field. This should involve marking at least one reference in the session.
            Assert.Equal(0, writer.Position);

            foreach (var value in TestValues)
            {
                var beforeReference = writer.Session.ReferencedObjects.CurrentReferenceId;
                writerCodec.WriteField(ref writer, 0, typeof(TValue), value);
                Assert.True(writer.Position > 0);

                writer.Commit();
                var afterReference = writer.Session.ReferencedObjects.CurrentReferenceId;
                Assert.True(beforeReference < afterReference, $"Writing a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
                if (value is null)
                {
                    Assert.True(beforeReference + 1 == afterReference, $"Writing a null field should result in exactly one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
                }

                stream.Flush();

                stream.Position = 0;
                var reader = Reader.Create(stream, readerSession);

                var previousPos = reader.Position;
                Assert.Equal(0, previousPos);
                var readerCodec = CreateCodec();
                var readField = reader.ReadFieldHeader();

                Assert.True(reader.Position > previousPos);
                previousPos = reader.Position;

                beforeReference = reader.Session.ReferencedObjects.CurrentReferenceId;
                var readValue = readerCodec.ReadValue(ref reader, readField);

                Assert.True(reader.Position > previousPos);

                afterReference = reader.Session.ReferencedObjects.CurrentReferenceId;
                Assert.True(beforeReference < afterReference, $"Reading a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
                if (readValue is null)
                {
                    Assert.True(beforeReference + 1 == afterReference, $"Reading a null field should result in at exactly one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
                }
            }
        }

        [Fact]
        public void CorrectlyAdvancesReferenceCounter()
        {
            var pipe = new Pipe();
            using var writerSession = _sessionPool.GetSession(); 
            var writer = Writer.Create(pipe.Writer, writerSession);
            var writerCodec = CreateCodec();
            var beforeReference = writer.Session.ReferencedObjects.CurrentReferenceId;

            // Write the field. This should involve marking at least one reference in the session.
            Assert.Equal(0, writer.Position);

            writerCodec.WriteField(ref writer, 0, typeof(TValue), CreateValue());
            Assert.True(writer.Position > 0);

            writer.Commit();
            var afterReference = writer.Session.ReferencedObjects.CurrentReferenceId;
            Assert.True(beforeReference < afterReference, $"Writing a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
            _ = pipe.Writer.FlushAsync().AsTask().GetAwaiter().GetResult();
            pipe.Writer.Complete();

            _ = pipe.Reader.TryRead(out var readResult);
            using var readerSession = _sessionPool.GetSession(); 
            var reader = Reader.Create(readResult.Buffer, readerSession);

            var previousPos = reader.Position;
            Assert.Equal(0, previousPos);
            var readerCodec = CreateCodec();
            var readField = reader.ReadFieldHeader();

            Assert.True(reader.Position > previousPos);
            previousPos = reader.Position;

            beforeReference = reader.Session.ReferencedObjects.CurrentReferenceId;
            _ = readerCodec.ReadValue(ref reader, readField);

            Assert.True(reader.Position > previousPos);

            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
            afterReference = reader.Session.ReferencedObjects.CurrentReferenceId;
            Assert.True(beforeReference < afterReference, $"Reading a field should result in at least one reference being marked in the session. Before: {beforeReference}, After: {afterReference}");
        }

        [Fact]
        public void CanRoundTripViaSerializer_StreamPooled()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in TestValues)
            {
                var buffer = new MemoryStream();

                var writer = Writer.CreatePooled(buffer, _sessionPool.GetSession());
                serializer.Serialize(original, ref writer);
                buffer.Flush();
                writer.Output.Dispose();

                buffer.Position = 0;
                var reader = Reader.Create(buffer, _sessionPool.GetSession());
                var deserialized = serializer.Deserialize(ref reader);

                Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
            }
        }

        [Fact]
        public void CanRoundTripViaSerializer_Span()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in TestValues)
            {
                var buffer = new byte[8096].AsSpan();

                var writer = Writer.Create(buffer, _sessionPool.GetSession());
                serializer.Serialize(original, ref writer);

                var reader = Reader.Create(buffer, _sessionPool.GetSession());
                var deserialized = serializer.Deserialize(ref reader);

                Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
            }
        }

        [Fact]
        public void CanRoundTripViaSerializer_Array()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in TestValues)
            {
                var buffer = new byte[8096];

                var writer = Writer.Create(buffer, _sessionPool.GetSession());
                serializer.Serialize(original, ref writer);

                var reader = Reader.Create(buffer, _sessionPool.GetSession());
                var deserialized = serializer.Deserialize(ref reader);

                Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
            }
        }

        [Fact]
        public void CanRoundTripViaSerializer_Memory()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in TestValues)
            {
                var buffer = (new byte[8096]).AsMemory();

                var writer = Writer.Create(buffer, _sessionPool.GetSession());
                serializer.Serialize(original, ref writer);

                var reader = Reader.Create(buffer, _sessionPool.GetSession());
                var deserialized = serializer.Deserialize(ref reader);

                Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
            }
        }

        [Fact]
        public void CanRoundTripViaSerializer_MemoryStream()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in TestValues)
            {
                var buffer = new MemoryStream();

                using var writerSession = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, writerSession);
                serializer.Serialize(original, ref writer);
                buffer.Flush();_sessionPool.GetSession();
                buffer.SetLength(buffer.Position);

                buffer.Position = 0;
                using var readerSession = _sessionPool.GetSession();
                var reader = Reader.Create(buffer, readerSession);
                var deserialized = serializer.Deserialize(ref reader);

                Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
                Assert.Equal(writer.Position, reader.Position);
                Assert.Equal(writerSession.ReferencedObjects.CurrentReferenceId, readerSession.ReferencedObjects.CurrentReferenceId);
            }
        }

        [Fact]
        public void ProducesValidBitStream()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();
            foreach (var value in TestValues)
            {
                var array = serializer.SerializeToArray(value);
                using var session = _sessionPool.GetSession();
                var reader = Reader.Create(array, session);
                var formatted = new StringBuilder();
                try
                {
                    BitStreamFormatter.Format(ref reader, formatted);
                    Assert.True(formatted.ToString() is string { Length: > 0 });
                }
                catch (Exception exception)
                {
                    Assert.True(false, $"Formatting failed with exception: {exception} and partial result: \"{formatted}\"");
                }
            }
        }

        [Fact]
        public void WritersProduceSameResults()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in TestValues)
            {
                byte[] expected;

                {
                    var buffer = new TestMultiSegmentBufferWriter(1024);
                    var writer = Writer.Create(buffer, _sessionPool.GetSession());
                    serializer.Serialize(original, ref writer);
                    expected = buffer.GetReadOnlySequence(0).ToArray();
                }

                {
                    var buffer = new MemoryStream();
                    var writer = Writer.Create(buffer, _sessionPool.GetSession());
                    serializer.Serialize(original, ref writer);
                    buffer.Flush();
                    buffer.SetLength(buffer.Position);
                    buffer.Position = 0;
                    var result = buffer.ToArray();
                    Assert.Equal(expected, result);
                }

                {
                    var buffer = (new byte[8096]).AsMemory();
                    var writer = Writer.Create(buffer, _sessionPool.GetSession());
                    serializer.Serialize(original, ref writer);
                    var result = buffer.Slice(0, writer.Output.BytesWritten).ToArray();
                    Assert.Equal(expected, result);
                }

                {
                    var buffer = (new byte[8096]).AsSpan();
                    var writer = Writer.Create(buffer, _sessionPool.GetSession());
                    serializer.Serialize(original, ref writer);
                    var result = buffer.Slice(0, writer.Output.BytesWritten).ToArray();
                    Assert.Equal(expected, result);
                }

                {
                    var buffer = new byte[8096];
                    var writer = Writer.Create(buffer, _sessionPool.GetSession());
                    serializer.Serialize(original, ref writer);
                    var result = buffer.AsSpan(0, writer.Output.BytesWritten).ToArray();
                    Assert.Equal(expected, result);
                }

                {
                    var buffer = new MemoryStream();
                    var writer = Writer.CreatePooled(buffer, _sessionPool.GetSession());
                    serializer.Serialize(original, ref writer);
                    buffer.Flush();
                    buffer.SetLength(buffer.Position);
                    buffer.Position = 0;
                    var result = buffer.ToArray();
                    writer.Output.Dispose();
                    Assert.Equal(expected, result);
                }

                {
                    var buffer = new MemoryStream();
                    var writer = Writer.Create((Stream)buffer, _sessionPool.GetSession());
                    serializer.Serialize(original, ref writer);
                    buffer.Flush();
                    buffer.SetLength(buffer.Position);
                    buffer.Position = 0;
                    var result = buffer.ToArray();
                    Assert.Equal(expected, result);
                }
            }
        }

        [Fact]
        public void CanRoundTripViaSerializer()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<TValue>>();

            foreach (var original in TestValues)
            {
                var buffer = new TestMultiSegmentBufferWriter(1024);

                using var writerSession = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, writerSession);
                serializer.Serialize(original, ref writer);

                using var readerSession = _sessionPool.GetSession();
                var reader = Reader.Create(buffer.GetReadOnlySequence(0), readerSession);
                var deserialized = serializer.Deserialize(ref reader);

                Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
                Assert.Equal(writer.Position, reader.Position);
                Assert.Equal(writerSession.ReferencedObjects.CurrentReferenceId, readerSession.ReferencedObjects.CurrentReferenceId);
            }
        }

        [Fact]
        public void CanRoundTripViaObjectSerializer()
        {
            var serializer = _serviceProvider.GetRequiredService<Serializer<object>>();

            foreach (var original in TestValues)
            {
                var buffer = new TestSingleSegmentBufferWriter(new byte[10240]);

                using var writerSession = _sessionPool.GetSession();
                var writer = Writer.Create(buffer, writerSession);
                serializer.Serialize(original, ref writer);

                using var readerSession = _sessionPool.GetSession();
                var reader = Reader.Create(buffer.GetReadOnlySequence(0), readerSession);
                var deserializedObject = serializer.Deserialize(ref reader);
                if (original != null && !typeof(TValue).IsEnum)
                {
                    var deserialized = Assert.IsAssignableFrom<TValue>(deserializedObject);
                    Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
                }
                else if (typeof(TValue).IsEnum)
                {
                    var deserialized = (TValue)deserializedObject;
                    Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
                }
                else
                {
                    Assert.Null(deserializedObject);
                }

                Assert.Equal(writer.Position, reader.Position);
                Assert.Equal(writerSession.ReferencedObjects.CurrentReferenceId, readerSession.ReferencedObjects.CurrentReferenceId);
            }
        }

        [Fact]
        public void RoundTrippedValuesEqual() => TestRoundTrippedValue(CreateValue());

        [Fact]
        public void CanRoundTripDefaultValueViaCodec() => TestRoundTrippedValue(default);

        [Fact]
        public void CanSkipValue() => CanBeSkipped(default);

        [Fact]
        public void CanSkipDefaultValue() => CanBeSkipped(default);

        [Fact]
        public void CorrectlyHandlesBuffers()
        {
            var testers = BufferTestHelper<TValue>.GetTestSerializers(_serviceProvider);

            foreach (var tester in testers)
            {
                foreach (var maxSegmentSize in MaxSegmentSizes)
                {
                    foreach (var value in TestValues)
                    {
                        var buffer = tester.Serialize(value);
                        var sequence = buffer.GetReadOnlySequence(maxSegmentSize);
                        tester.Deserialize(sequence, out var output);
                        var bufferWriterType = tester.GetType().BaseType?.GenericTypeArguments[1];
                        Assert.True(Equals(value, output),
                            $"Deserialized value {output} must be equal to serialized value {value}. " +
                            $"IBufferWriter<> type: {bufferWriterType}, Max Read Segment Size: {maxSegmentSize}. " +
                            $"Buffer: 0x{string.Join(" ", sequence.ToArray().Select(b => $"{b:X2}"))}");
                    }
                }
            }
        }

        private void CanBeSkipped(TValue original)
        {
            var pipe = new Pipe();
            using var writerSession = _sessionPool.GetSession();
            var writer = Writer.Create(pipe.Writer, writerSession);
            var writerCodec = CreateCodec();
            writerCodec.WriteField(ref writer, 0, typeof(TValue), original);
            var expectedLength = writer.Position;
            writer.Commit();
            _ = pipe.Writer.FlushAsync().AsTask().GetAwaiter().GetResult();
            pipe.Writer.Complete();

            _ = pipe.Reader.TryRead(out var readResult);

            {
                using var readerSession = _sessionPool.GetSession();
                var reader = Reader.Create(readResult.Buffer, readerSession);
                var readField = reader.ReadFieldHeader();
                reader.SkipField(readField);
                Assert.Equal(expectedLength, reader.Position);
                Assert.Equal(writerSession.ReferencedObjects.CurrentReferenceId, readerSession.ReferencedObjects.CurrentReferenceId);
            }

            {
                var codec = new SkipFieldCodec();
                using var readerSession = _sessionPool.GetSession();
                var reader = Reader.Create(readResult.Buffer, readerSession);
                var readField = reader.ReadFieldHeader();
                var shouldBeNull = codec.ReadValue(ref reader, readField);
                Assert.Null(shouldBeNull);
                Assert.Equal(expectedLength, reader.Position);
                Assert.Equal(writerSession.ReferencedObjects.CurrentReferenceId, readerSession.ReferencedObjects.CurrentReferenceId);
            }

            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
        }

        private void TestRoundTrippedValue(TValue original)
        {
            var pipe = new Pipe();
            using var writerSession = _sessionPool.GetSession();
            var writer = Writer.Create(pipe.Writer, writerSession);
            var writerCodec = CreateCodec();
            writerCodec.WriteField(ref writer, 0, typeof(TValue), original);
            writer.Commit();
            _ = pipe.Writer.FlushAsync().AsTask().GetAwaiter().GetResult();
            pipe.Writer.Complete();

            _ = pipe.Reader.TryRead(out var readResult);
            using var readerSession = _sessionPool.GetSession();
            var reader = Reader.Create(readResult.Buffer, readerSession);
            var readerCodec = CreateCodec();
            var readField = reader.ReadFieldHeader();
            var deserialized = readerCodec.ReadValue(ref reader, readField);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
            Assert.True(Equals(original, deserialized), $"Deserialized value \"{deserialized}\" must equal original value \"{original}\"");
            Assert.Equal(writerSession.ReferencedObjects.CurrentReferenceId, readerSession.ReferencedObjects.CurrentReferenceId);
        }
    }
}