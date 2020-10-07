using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Benchmarks.Models;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;
using Hyperion;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using SerializerSession = Hagar.Session.SerializerSession;
using Utf8JsonNS = Utf8Json;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    //[DisassemblyDiagnoser(recursiveDepth: 2, printSource: true)]
    //[EtwProfiler]
    public class ClassDeserializeBenchmark
    {
        private static readonly MemoryStream ProtoInput;

        private static readonly byte[] MsgPackInput = MessagePack.MessagePackSerializer.Serialize(IntClass.Create());

        private static readonly string NewtonsoftJsonInput = JsonConvert.SerializeObject(IntClass.Create());

        private static readonly byte[] SpanJsonInput = SpanJson.JsonSerializer.Generic.Utf8.Serialize(IntClass.Create());

        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] { typeof(IntClass) }));
        private static readonly MemoryStream HyperionInput;

        private static readonly Serializer<IntClass> HagarSerializer;
        private static readonly byte[] HagarInput;
        private static readonly SerializerSession Session;

        private static readonly HagarGen_Serializer_IntClass_1843466 Generated = new HagarGen_Serializer_IntClass_1843466();
        private static readonly DeserializerSession HyperionSession;

        private static readonly Utf8JsonNS.IJsonFormatterResolver Utf8JsonResolver = Utf8JsonNS.Resolvers.StandardResolver.Default;
        private static readonly byte[] Utf8JsonInput;

        private static readonly byte[] SystemTextJsonInput;

        static ClassDeserializeBenchmark()
        {
            ProtoInput = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoInput, IntClass.Create());

            HyperionInput = new MemoryStream();
            HyperionSession = HyperionSerializer.GetDeserializerSession();
            HyperionSerializer.Serialize(IntClass.Create(), HyperionInput);

            // Hagar
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntClass>>();
            var bytes = new byte[1000];
            Session = services.GetRequiredService<SessionPool>().GetSession();
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            //HagarSerializer.Serialize(ref writer, IntClass.Create());
            writer.WriteStartObject(0, typeof(IntClass), typeof(IntClass));
            Generated.Serialize(ref writer, IntClass.Create());
            writer.WriteEndObject();
            HagarInput = bytes;

            Utf8JsonInput = Utf8JsonNS.JsonSerializer.Serialize(IntClass.Create(), Utf8JsonResolver);

            var stream = new MemoryStream();
            using (var jsonWriter = new System.Text.Json.Utf8JsonWriter(stream))
            {
                System.Text.Json.JsonSerializer.Serialize<IntClass>(jsonWriter, IntClass.Create());
            }

            SystemTextJsonInput = stream.ToArray();
        }

        private static int SumResult(IntClass result) => result.MyProperty1 +
                   result.MyProperty2 +
                   result.MyProperty3 +
                   result.MyProperty4 +
                   result.MyProperty5 +
                   result.MyProperty6 +
                   result.MyProperty7 +
                   result.MyProperty8 +
                   result.MyProperty9;

        [Fact]
        [Benchmark(Baseline = true)]
        public int Hagar()
        {
            Session.FullReset();
            var instance = HagarSerializer.Deserialize(HagarInput, Session);
            return SumResult(instance);
        }
        
        [Benchmark]
        public int HagarHandCrafted()
        {
            Session.FullReset();
            var reader = Reader.Create(HagarInput, Session);
            var instance = IntClass.Create();
            _ = reader.ReadFieldHeader();
            Generated.DeserializeNew(ref reader, instance);
            //var instance = HagarSerializer.Deserialize(ref reader);
            return SumResult(instance);
        }

        [Benchmark]
        public int Utf8Json() => SumResult(Utf8JsonNS.JsonSerializer.Deserialize<IntClass>(Utf8JsonInput, Utf8JsonResolver));

        [Benchmark]
        public int SystemTextJson() => SumResult(System.Text.Json.JsonSerializer.Deserialize<IntClass>(SystemTextJsonInput));

        [Benchmark]
        public int MessagePackCSharp() => SumResult(MessagePack.MessagePackSerializer.Deserialize<IntClass>(MsgPackInput));

        [Benchmark]
        public int ProtobufNet()
        {
            ProtoInput.Position = 0;
            return SumResult(ProtoBuf.Serializer.Deserialize<IntClass>(ProtoInput));
        }

        [Benchmark]
        public int Hyperion()
        {
            HyperionInput.Position = 0;

            return SumResult(HyperionSerializer.Deserialize<IntClass>(HyperionInput, HyperionSession));
        }

        [Benchmark]
        public int NewtonsoftJson() => SumResult(JsonConvert.DeserializeObject<IntClass>(NewtonsoftJsonInput));

        [Benchmark(Description = "SpanJson")]
        public int SpanJsonUtf8() => SumResult(SpanJson.JsonSerializer.Generic.Utf8.Deserialize<IntClass>(SpanJsonInput));
    }

    internal sealed class HagarGen_Serializer_IntClass_1843466 : global::Hagar.Serializers.IPartialSerializer<global::Benchmarks.Models.IntClass>
    {
        private static readonly global::System.Type Int32Type = typeof(int);
        public HagarGen_Serializer_IntClass_1843466()
        {
        }

        public void Serialize<TBufferWriter>(ref global::Hagar.Buffers.Writer<TBufferWriter> writer, global::Benchmarks.Models.IntClass instance)
            where TBufferWriter : global::System.Buffers.IBufferWriter<byte>
        {
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 0U, Int32Type, instance.MyProperty1);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty2);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty3);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty4);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty5);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty6);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty7);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty8);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, Int32Type, instance.MyProperty9);
        }

        public void Deserialize<TInput>(ref global::Hagar.Buffers.Reader<TInput> reader, global::Benchmarks.Models.IntClass instance)
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
                switch ((fieldId))
                {
                    case 0U:
                        instance.MyProperty1 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 1U:
                        instance.MyProperty2 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 2U:
                        instance.MyProperty3 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 3U:
                        instance.MyProperty4 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 4U:
                        instance.MyProperty5 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 5U:
                        instance.MyProperty6 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 6U:
                        instance.MyProperty7 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 7U:
                        instance.MyProperty8 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 8U:
                        instance.MyProperty9 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }
        }
        public void DeserializeNew<TInput>(ref global::Hagar.Buffers.Reader<TInput> reader, global::Benchmarks.Models.IntClass instance)
        {
            int id = 0;
            Field header = default;
            while (true)
            {
                id = ReadHeader(ref reader, ref header, id);

                if (id == 0)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty1 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 1)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty2 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 2)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty3 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 3)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty4 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 4)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty5 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 5)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty6 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 6)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty7 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 7)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty8 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 8)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty9 = reader.ReadInt32(header.WireType);
                    id = ReadHeaderExpectingEndBaseOrEndObject(ref reader, ref header, id);
                }

                if (id == -1)
                {
                    break;
                }

                reader.ConsumeUnknownField(header);
            }
        }

        private static int ReadHeader<TInput>(ref Reader<TInput> reader, ref Field header, int id)
        {
            reader.ReadFieldHeader(ref header);
            if (header.IsEndBaseOrEndObject)
            {
                return -1;
            }

            return (int)(id + header.FieldIdDelta);
        }

        private static int ReadHeaderExpectingEndBaseOrEndObject<TInput>(ref Reader<TInput> reader, ref Field header, int id)
        {
            reader.ReadFieldHeader(ref header);
            if (header.IsEndBaseOrEndObject)
            {
                return -1;
            }

            return (int)(id + header.FieldIdDelta);
        }
    }
}