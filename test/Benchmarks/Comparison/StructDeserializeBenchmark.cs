using BenchmarkDotNet.Attributes;
using Benchmarks.Models;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;
using Hyperion;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.IO;
using Xunit;
using ZeroFormatter;
using SerializerSession = Hagar.Session.SerializerSession;
using Utf8JsonNS = Utf8Json;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    //[DisassemblyDiagnoser(recursiveDepth: 4)]
    //[EtwProfiler]
    public class StructDeserializeBenchmark
    {
        private static readonly MemoryStream ProtoInput;
        private static readonly string NewtonsoftJsonInput = JsonConvert.SerializeObject(IntStruct.Create());

        private static readonly byte[] SpanJsonInput = SpanJson.JsonSerializer.Generic.Utf8.Serialize(IntStruct.Create());

        private static readonly byte[] MsgPackInput = MessagePack.MessagePackSerializer.Serialize(IntStruct.Create());
        private static readonly byte[] ZeroFormatterInput = ZeroFormatterSerializer.Serialize(IntStruct.Create());

        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] { typeof(IntStruct) }));
        private static readonly MemoryStream HyperionInput;
        private static readonly Hyperion.DeserializerSession HyperionSession;

        private static readonly ValueSerializer<IntStruct> HagarSerializer;
        private static readonly HagarGen_Serializer_IntStruct_1843466 HagarHandCraftedSerializer;
        private static readonly byte[] HagarInput;
        private static readonly SerializerSession Session;

        private static readonly Utf8JsonNS.IJsonFormatterResolver Utf8JsonResolver = Utf8JsonNS.Resolvers.StandardResolver.Default;
        private static readonly byte[] Utf8JsonInput;
        private static readonly byte[] SystemTextJsonInput;

        static StructDeserializeBenchmark()
        {
            ProtoInput = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoInput, IntStruct.Create());

            HyperionInput = new MemoryStream();
            HyperionSerializer.Serialize(IntStruct.Create(), HyperionInput);

            // Hagar
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<ValueSerializer<IntStruct>>();
            Session = services.GetRequiredService<SessionPool>().GetSession();
            var bytes = new byte[1000];
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            IntStruct intStruct = IntStruct.Create();
            HagarSerializer.Serialize(ref intStruct, ref writer);
            HagarHandCraftedSerializer = new HagarGen_Serializer_IntStruct_1843466();
            HagarInput = bytes;

            HyperionSession = HyperionSerializer.GetDeserializerSession();

            Utf8JsonInput = Utf8JsonNS.JsonSerializer.Serialize(IntStruct.Create(), Utf8JsonResolver);

            var stream = new MemoryStream();
            using (var jsonWriter = new System.Text.Json.Utf8JsonWriter(stream))
            {
                System.Text.Json.JsonSerializer.Serialize<IntStruct>(jsonWriter, IntStruct.Create());
            }

            SystemTextJsonInput = stream.ToArray();

        }

        private static int SumResult(in IntStruct result) => result.MyProperty1 +
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
            IntStruct result = default;
            HagarSerializer.Deserialize(HagarInput, ref result, Session);
            return SumResult(in result);
        }

        [Benchmark]
        public int HagarHandCrafted()
        {
            Session.FullReset();
            IntStruct result = default;
            var reader = Reader.Create(HagarInput, Session);
            Field ignored = default;
            reader.ReadFieldHeader(ref ignored);
            HagarHandCraftedSerializer.Deserialize(ref reader, ref result);
            return SumResult(in result);
        }

        [Benchmark]
        public int Utf8Json() => SumResult(Utf8JsonNS.JsonSerializer.Deserialize<IntStruct>(Utf8JsonInput, Utf8JsonResolver));

        [Benchmark]
        public int SystemTextJson() => SumResult(System.Text.Json.JsonSerializer.Deserialize<IntStruct>(SystemTextJsonInput));

        [Benchmark]
        public int MessagePackCSharp() => SumResult(MessagePack.MessagePackSerializer.Deserialize<IntStruct>(MsgPackInput));

        [Benchmark]
        public int ProtobufNet()
        {
            ProtoInput.Position = 0;
            return SumResult(ProtoBuf.Serializer.Deserialize<IntStruct>(ProtoInput));
        }

        [Benchmark]
        public int Hyperion()
        {
            HyperionInput.Position = 0;
            return SumResult(HyperionSerializer.Deserialize<IntStruct>(HyperionInput, HyperionSession));
        }

        //[Benchmark]
        public int ZeroFormatter() => SumResult(ZeroFormatterSerializer.Deserialize<IntStruct>(ZeroFormatterInput));

        [Benchmark]
        public int NewtonsoftJson() => SumResult(JsonConvert.DeserializeObject<IntStruct>(NewtonsoftJsonInput));

        [Benchmark(Description = "SpanJson")]
        public int SpanJsonUtf8() => SumResult(SpanJson.JsonSerializer.Generic.Utf8.Deserialize<IntStruct>(SpanJsonInput));

        internal sealed class HagarGen_Serializer_IntStruct_1843466 : global::Hagar.Serializers.IValueSerializer<global::Benchmarks.Models.IntStruct>
        {
            private static readonly global::System.Type Int32Type = typeof(int);
            public HagarGen_Serializer_IntStruct_1843466()
            {
            }

            public void Serialize<TBufferWriter>(ref global::Hagar.Buffers.Writer<TBufferWriter> writer, ref global::Benchmarks.Models.IntStruct instance)
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

            public void Deserialize<TInput>(ref global::Hagar.Buffers.Reader<TInput> reader, ref global::Benchmarks.Models.IntStruct instance)
            {
                int id = 0;
                Field header = default;
                while (true)
                {
                    id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);

                    if (id == 0)
                    {
                        instance.MyProperty1 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 1)
                    {
                        instance.MyProperty2 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 2)
                    {
                        instance.MyProperty3 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 3)
                    {
                        instance.MyProperty4 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 4)
                    {
                        instance.MyProperty5 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 5)
                    {
                        instance.MyProperty6 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 6)
                    {
                        instance.MyProperty7 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 7)
                    {
                        instance.MyProperty8 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                    }

                    if (id == 8)
                    {
                        instance.MyProperty9 = Int32Codec.ReadValue(ref reader, header);
                        id = HagarGeneratedCodeHelper.ReadHeaderExpectingEndBaseOrEndObject(ref reader, ref header, id);
                    }

                    if (id == -1)
                    {
                        break;
                    }

                    reader.ConsumeUnknownField(header);
                }
            }
        }
    } 
}