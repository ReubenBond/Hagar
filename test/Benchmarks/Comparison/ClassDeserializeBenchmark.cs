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

        private static readonly Hyperion.Serializer HyperionSerializer = new(new SerializerOptions(knownTypes: new[] { typeof(IntClass) }));
        private static readonly MemoryStream HyperionInput;

        private static readonly Serializer<IntClass> HagarSerializer;
        private static readonly byte[] HagarInput;
        private static readonly SerializerSession Session;

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
                .AddHagar()
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntClass>>();
            var bytes = new byte[1000];
            Session = services.GetRequiredService<SerializerSessionPool>().GetSession();
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            HagarSerializer.Serialize(IntClass.Create(), ref writer);
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
}