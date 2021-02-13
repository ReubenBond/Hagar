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
            Session = services.GetRequiredService<SerializerSessionPool>().GetSession();
            var bytes = new byte[1000];
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            IntStruct intStruct = IntStruct.Create();
            HagarSerializer.Serialize(ref intStruct, ref writer);
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
    } 
}