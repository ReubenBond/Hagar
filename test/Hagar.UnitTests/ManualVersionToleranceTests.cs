using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Hagar.UnitTests
{
    public class ManualVersionToleranceTests
    {
        private const string TestString = "hello, hagar";
        private readonly ITestOutputHelper _log;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFieldCodec<SubType> _serializer;

        public ManualVersionToleranceTests(ITestOutputHelper log)
        {
            _log = log;
            var serviceCollection = new ServiceCollection();
            _ = serviceCollection.AddHagar(builder =>
              {
                  _ = builder.Configure(configuration =>
                    {
                        _ = configuration.Serializers.Add(typeof(SubTypeSerializer));
                        _ = configuration.Serializers.Add(typeof(BaseTypeSerializer));
                    });
              });
            //serviceCollection.AddSingleton<IGeneralizedCodec, DotNetSerializableCodec>();
            //serviceCollection.AddSingleton<IGeneralizedCodec, JsonCodec>();

            _serviceProvider = serviceCollection.BuildServiceProvider();

            var codecProvider = _serviceProvider.GetRequiredService<CodecProvider>();
            _serializer = codecProvider.GetCodec<SubType>();
        }

        [Fact]
        public void VersionTolerance_RoundTrip_Tests()
        {
            RoundTripTest(
                new SubType
                {
                    BaseTypeString = "HOHOHO",
                    AddedLaterString = TestString,
                    String = null,
                    Int = 1,
                    Ref = TestString
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
                    BaseTypeString = TestString,
                    String = TestString,
                    Int = 10
                });

            RoundTripTest(
                new SubType
                {
                    BaseTypeString = TestString,
                    String = null,
                    Int = 1
                });

            RoundTripTest(
                new SubType
                {
                    BaseTypeString = TestString,
                    String = null,
                    Int = 1
                });

            TestSkip(
                new SubType
                {
                    BaseTypeString = TestString,
                    String = null,
                    Int = 1
                });

            var self = new SubType
            {
                BaseTypeString = "HOHOHO",
                AddedLaterString = TestString,
                String = null,
                Int = 1
            };
            self.Ref = self;
            RoundTripTest(self);

            self.Ref = Guid.NewGuid();
            RoundTripTest(self);
        }

        private SerializerSession GetSession() => _serviceProvider.GetRequiredService<SessionPool>().GetSession();

        private void RoundTripTest(SubType expected)
        {
            using var writerSession = GetSession();
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, writerSession);

            _serializer.WriteField(ref writer, 0, typeof(SubType), expected);
            writer.Commit();

            _log.WriteLine($"Size: {writer.Position} bytes.");
            _log.WriteLine($"Wrote References:\n{GetWriteReferenceTable(writerSession)}");

            _ = pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            _ = pipe.Reader.TryRead(out var readResult);
            using var readerSesssion = GetSession();
            var reader = new Reader(readResult.Buffer, readerSesssion);
            var initialHeader = reader.ReadFieldHeader();

            _log.WriteLine("Header:");
            _log.WriteLine(initialHeader.ToString());

            var actual = _serializer.ReadValue(ref reader, initialHeader);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();

            _log.WriteLine($"Expect: {expected}\nActual: {actual}");

            Assert.Equal(expected.BaseTypeString, actual.BaseTypeString);
            Assert.Null(actual.AddedLaterString); // The deserializer isn't 'aware' of this field which was added later - version tolerance.
            Assert.Equal(expected.String, actual.String);
            Assert.Equal(expected.Int, actual.Int);

            var references = GetReadReferenceTable(reader.Session);
            _log.WriteLine($"Read references:\n{references}");
        }

        private void TestSkip(SubType expected)
        {
            using var writerSession = GetSession();
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, writerSession);

            _serializer.WriteField(ref writer, 0, typeof(SubType), expected);
            writer.Commit();

            _ = pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            _ = pipe.Reader.TryRead(out var readResult);
            using var readerSession = GetSession();
            var reader = new Reader(readResult.Buffer, readerSession);
            var initialHeader = reader.ReadFieldHeader();
            var skipCodec = new SkipFieldCodec();
            _ = skipCodec.ReadValue(ref reader, initialHeader);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
            _log.WriteLine($"Skipped {reader.Position} bytes.");
        }

        private static StringBuilder GetReadReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyReferenceTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                _ = references.AppendLine($"\t[{entry.Key}] {entry.Value}");
            }
            return references;
        }

        private static StringBuilder GetWriteReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyIdTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                _ = references.AppendLine($"\t[{entry.Value}] {entry.Key}");
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

            public bool Equals(BaseType other) => other is object
                    && string.Equals(BaseTypeString, other.BaseTypeString, StringComparison.Ordinal)
                    && string.Equals(AddedLaterString, other.AddedLaterString, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is BaseType baseType && Equals(baseType);

            public override int GetHashCode() => HashCode.Combine(BaseTypeString, AddedLaterString);

            public override string ToString() => $"{nameof(BaseTypeString)}: {BaseTypeString}";
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
                if (other is null)
                {
                    return false;
                }

                return
                    base.Equals(other)
                    && string.Equals(String, other.String, StringComparison.Ordinal)
                    && Int == other.Int
                    && (ReferenceEquals(Ref, other.Ref) || Ref.Equals(other.Ref));
            }

            public override string ToString()
            {
                string refString = Ref == this ? "[this]" : $"[{Ref?.ToString() ?? "null"}]";
                return $"{base.ToString()}, {nameof(String)}: {String}, {nameof(Int)}: {Int}, Ref: {refString}";
            }

            public override bool Equals(object obj) => obj is SubType subType && Equals(subType);

            public override int GetHashCode()
            {
                // Avoid stack overflows with this one weird trick.
                if (ReferenceEquals(Ref, this))
                {
                    return HashCode.Combine(base.GetHashCode(), String, Int);
                }

                return HashCode.Combine(base.GetHashCode(), String, Int, Ref);
            }
        }

        public class SubTypeSerializer : IPartialSerializer<SubType>
        {
            private readonly IPartialSerializer<BaseType> _baseTypeSerializer;
            private readonly IFieldCodec<string> _stringCodec;
            private readonly IFieldCodec<int> _intCodec;
            private readonly IFieldCodec<object> _objectCodec;

            public SubTypeSerializer(IPartialSerializer<BaseType> baseTypeSerializer, IFieldCodec<string> stringCodec, IFieldCodec<int> intCodec, IFieldCodec<object> objectCodec)
            {
                _baseTypeSerializer = HagarGeneratedCodeHelper.UnwrapService(this, baseTypeSerializer);
                _stringCodec = HagarGeneratedCodeHelper.UnwrapService(this, stringCodec);
                _intCodec = HagarGeneratedCodeHelper.UnwrapService(this, intCodec);
                _objectCodec = HagarGeneratedCodeHelper.UnwrapService(this, objectCodec);
            }

            public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, SubType obj) where TBufferWriter : IBufferWriter<byte>
            {
                _baseTypeSerializer.Serialize(ref writer, obj);
                writer.WriteEndBase(); // the base object is complete.

                _stringCodec.WriteField(ref writer, 0, typeof(string), obj.String);
                _intCodec.WriteField(ref writer, 1, typeof(int), obj.Int);
                _objectCodec.WriteField(ref writer, 1, typeof(object), obj.Ref);
                _intCodec.WriteField(ref writer, 1, typeof(int), obj.Int);
                _intCodec.WriteField(ref writer, 409, typeof(int), obj.Int);
                /*writer.WriteFieldHeader(session, 1025, typeof(Guid), Guid.Empty.GetType(), WireType.Fixed128);
                writer.WriteFieldHeader(session, 1020, typeof(object), typeof(Program), WireType.Reference);*/
            }

            public void Deserialize(ref Reader reader, SubType obj)
            {
                uint fieldId = 0;
                _baseTypeSerializer.Deserialize(ref reader, obj);
                while (true)
                {
                    var header = reader.ReadFieldHeader();
                    if (header.IsEndBaseOrEndObject)
                    {
                        break;
                    }

                    fieldId += header.FieldIdDelta;
                    switch (fieldId)
                    {
                        case 0:
                            obj.String = _stringCodec.ReadValue(ref reader, header);
                            break;
                        case 1:
                            obj.Int = _intCodec.ReadValue(ref reader, header);
                            break;
                        case 2:
                            obj.Ref = _objectCodec.ReadValue(ref reader, header);
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
                    if (header.IsEndBaseOrEndObject)
                    {
                        break;
                    }

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