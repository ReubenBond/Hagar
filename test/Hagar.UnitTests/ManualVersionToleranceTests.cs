using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Hagar.UnitTests
{
    public class ManualVersionToleranceTests
    {
        private readonly ITestOutputHelper log;
        private string testString = "hello, hagar";
        private IServiceProvider serviceProvider;
        private IFieldCodec<SubType> serializer;

        public ManualVersionToleranceTests(ITestOutputHelper log)
        {
            this.log = log;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHagar(builder =>
            {
                builder.Configure(configuration =>
                {
                    configuration.Serializers.Add(typeof(SubTypeSerializer));
                    configuration.Serializers.Add(typeof(BaseTypeSerializer));
                });
            });
            //serviceCollection.AddSingleton<IGeneralizedCodec, DotNetSerializableCodec>();
            //serviceCollection.AddSingleton<IGeneralizedCodec, JsonCodec>();

            this.serviceProvider = serviceCollection.BuildServiceProvider();

            var codecProvider = this.serviceProvider.GetRequiredService<CodecProvider>();
            this.serializer = codecProvider.GetCodec<SubType>();
        }

        [Fact]
        public void VersionTolerance_RoundTrip_Tests()
        {
            RoundTripTest(
                new SubType
                {
                    BaseTypeString = "HOHOHO",
                    AddedLaterString = testString,
                    String = null,
                    Int = 1,
                    Ref = testString
                });

            RoundTripTest(
                new SubType
                {
                    BaseTypeString = "base",
                    String = "sub",
                    Int = 2,
                });

            RoundTripTest(
                new SubType
                {
                    BaseTypeString = "base",
                    String = "sub",
                    Int = int.MinValue,
                });

            RoundTripTest(
                new SubType
                {
                    BaseTypeString = testString,
                    String = testString,
                    Int = 10
                });

            RoundTripTest(
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });

            RoundTripTest(
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });

            TestSkip(
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });

            var self = new SubType
            {
                BaseTypeString = "HOHOHO",
                AddedLaterString = testString,
                String = null,
                Int = 1
            };
            self.Ref = self;
            RoundTripTest(self);

            self.Ref = Guid.NewGuid();
            RoundTripTest(self);
        }

        private SerializerSession GetSession() => this.serviceProvider.GetRequiredService<SessionPool>().GetSession();

        private void RoundTripTest(SubType expected)
        {
            using var writerSession = GetSession();
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, writerSession);

            serializer.WriteField(ref writer, 0, typeof(SubType), expected);
            writer.Commit();

            this.log.WriteLine($"Size: {writer.Position} bytes.");
            this.log.WriteLine($"Wrote References:\n{GetWriteReferenceTable(writerSession)}");

            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            pipe.Reader.TryRead(out var readResult);
            using var readerSesssion = GetSession();
            var reader = new Reader(readResult.Buffer, readerSesssion);
            var initialHeader = reader.ReadFieldHeader();

            this.log.WriteLine("Header:");
            this.log.WriteLine(initialHeader.ToString());

            var actual = serializer.ReadValue(ref reader, initialHeader);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();

            this.log.WriteLine($"Expect: {expected}\nActual: {actual}");
            
            Assert.Equal(expected.BaseTypeString, actual.BaseTypeString);
            Assert.Null(actual.AddedLaterString); // The deserializer isn't 'aware' of this field which was added later - version tolerance.
            Assert.Equal(expected.String, actual.String);
            Assert.Equal(expected.Int, actual.Int);

            var references = GetReadReferenceTable(reader.Session);
            this.log.WriteLine($"Read references:\n{references}");
        }

        private void TestSkip(SubType expected)
        {
            using var writerSession = GetSession();
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, writerSession);

            this.serializer.WriteField(ref writer, 0, typeof(SubType), expected);
            writer.Commit();

            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            pipe.Reader.TryRead(out var readResult);
            using var readerSession = GetSession();
            var reader = new Reader(readResult.Buffer, readerSession);
            var initialHeader = reader.ReadFieldHeader();
            var skipCodec = new SkipFieldCodec();
            skipCodec.ReadValue(ref reader, initialHeader);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
            this.log.WriteLine($"Skipped {reader.Position} bytes.");
        }

        private static StringBuilder GetReadReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyReferenceTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                references.AppendLine($"\t[{entry.Key}] {entry.Value}");
            }
            return references;
        }

        private static StringBuilder GetWriteReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyIdTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                references.AppendLine($"\t[{entry.Value}] {entry.Key}");
            }
            return references;
        }

        /// <summary>
        /// NOTE: The serializer for this type is HAND-ROLLED. See <see cref="BaseTypeSerializer" />
        /// </summary>
        public class BaseType : IEquatable<BaseType>
        {
            public string BaseTypeString { get; set; }
            public string AddedLaterString { get; set; }

            public bool Equals(BaseType other)
            {
                return other is object
                    && string.Equals(this.BaseTypeString, other.BaseTypeString, StringComparison.Ordinal)
                    && string.Equals(this.AddedLaterString, other.AddedLaterString, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is BaseType baseType && this.Equals(baseType);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.BaseTypeString, this.AddedLaterString);
            }

            public override string ToString()
            {
                return $"{nameof(this.BaseTypeString)}: {this.BaseTypeString}";
            }
        }

        /// <summary>
        /// NOTE: The serializer for this type is HAND-ROLLED. See <see cref="SubTypeSerializer" />
        /// </summary>
        public class SubType : BaseType, IEquatable<SubType>
        {
            // 0
            public string String { get; set; }

            // 1
            public int Int { get; set; }

            // 3
            public object Ref { get; set; }

            public bool Equals(SubType other)
            {
                if (other is null) return false;
                return
                    base.Equals(other)
                    && string.Equals(this.String, other.String, StringComparison.Ordinal)
                    && this.Int == other.Int
                    && (ReferenceEquals(this.Ref, other.Ref) || this.Ref.Equals(other.Ref));
            }

            public override string ToString()
            {
                string refString = this.Ref == this ? "[this]" : $"[{this.Ref?.ToString() ?? "null"}]";
                return $"{base.ToString()}, {nameof(this.String)}: {this.String}, {nameof(this.Int)}: {this.Int}, Ref: {refString}";
            }

            public override bool Equals(object obj)
            {
                return obj is SubType subType && this.Equals(subType);
            }

            public override int GetHashCode()
            {
                // Avoid stack overflows with this one weird trick.
                if (ReferenceEquals(this.Ref, this)) return HashCode.Combine(base.GetHashCode(), this.String, this.Int);
                return HashCode.Combine(base.GetHashCode(), this.String, this.Int, this.Ref);
            }
        }

        public class SubTypeSerializer : IPartialSerializer<SubType>
        {
            private readonly IPartialSerializer<BaseType> baseTypeSerializer;
            private readonly IFieldCodec<string> stringCodec;
            private readonly IFieldCodec<int> intCodec;
            private readonly IFieldCodec<object> objectCodec;

            public SubTypeSerializer(IPartialSerializer<BaseType> baseTypeSerializer, IFieldCodec<string> stringCodec, IFieldCodec<int> intCodec, IFieldCodec<object> objectCodec)
            {
                this.baseTypeSerializer = HagarGeneratedCodeHelper.UnwrapService(this, baseTypeSerializer);
                this.stringCodec = HagarGeneratedCodeHelper.UnwrapService(this, stringCodec);
                this.intCodec = HagarGeneratedCodeHelper.UnwrapService(this, intCodec);
                this.objectCodec = HagarGeneratedCodeHelper.UnwrapService(this, objectCodec);
            }

            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, SubType obj) where TBufferWriter : IBufferWriter<byte>
            {
                this.baseTypeSerializer.Serialize(ref writer, obj);
                writer.WriteEndBase(); // the base object is complete.

                this.stringCodec.WriteField(ref writer, 0, typeof(string), obj.String);
                this.intCodec.WriteField(ref writer, 1, typeof(int), obj.Int);
                this.objectCodec.WriteField(ref writer, 1, typeof(object), obj.Ref);
                this.intCodec.WriteField(ref writer, 1, typeof(int), obj.Int);
                this.intCodec.WriteField(ref writer, 409, typeof(int), obj.Int);
                /*writer.WriteFieldHeader(session, 1025, typeof(Guid), Guid.Empty.GetType(), WireType.Fixed128);
                writer.WriteFieldHeader(session, 1020, typeof(object), typeof(Program), WireType.Reference);*/
            }

            public void Deserialize(ref Reader reader, SubType obj)
            {
                uint fieldId = 0;
                this.baseTypeSerializer.Deserialize(ref reader, obj);
                while (true)
                {
                    var header = reader.ReadFieldHeader();
                    if (header.IsEndBaseOrEndObject) break;
                    fieldId += header.FieldIdDelta;
                    switch (fieldId)
                    {
                        case 0:
                            obj.String = this.stringCodec.ReadValue(ref reader, header);
                            break;
                        case 1:
                            obj.Int = this.intCodec.ReadValue(ref reader, header);
                            break;
                        case 2:
                            obj.Ref = this.objectCodec.ReadValue(ref reader, header);
                            break;
                        default:
                            reader.ConsumeUnknownField(header);
                            break;
                    }
                }
            }
        }

        public class BaseTypeSerializer : IPartialSerializer<BaseType>
        {
            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, BaseType obj) where TBufferWriter : IBufferWriter<byte>
            {
                StringCodec.WriteField(ref writer, 0, typeof(string), obj.BaseTypeString);
                StringCodec.WriteField(ref writer, 234, typeof(string), obj.AddedLaterString);
            }

            public void Deserialize(ref Reader reader, BaseType obj)
            {
                uint fieldId = 0;
                while (true)
                {
                    var header = reader.ReadFieldHeader();
                    if (header.IsEndBaseOrEndObject) break;
                    fieldId += header.FieldIdDelta;
                    switch (fieldId)
                    {
                        case 0:
                            obj.BaseTypeString = StringCodec.ReadValue(ref reader, header);
                            break;
                        default:
                            reader.ConsumeUnknownField(header);
                            break;
                    }
                }
            }
        }
    }
}
