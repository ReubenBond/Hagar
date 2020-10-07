using BenchmarkDotNet.Attributes;
using Benchmarks.Models;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using Hyperion;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;
using ZeroFormatter;
using SerializerSession = Hagar.Session.SerializerSession;
using Utf8JsonNS = Utf8Json;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    [PayloadSizeColumn]
    public class StructSerializeBenchmark
    {
        private static readonly IntStruct Input = IntStruct.Create();

        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] { typeof(IntStruct) }));
        private static readonly Hyperion.SerializerSession HyperionSession;

        private static readonly Serializer<IntStruct> HagarSerializer;
        private static readonly byte[] HagarData;
        private static readonly SerializerSession Session;
        private static readonly MemoryStream ProtoBuffer = new MemoryStream();
        private static readonly MemoryStream HyperionBuffer = new MemoryStream();

        private static readonly MemoryStream Utf8JsonOutput = new MemoryStream();
        private static readonly Utf8JsonNS.IJsonFormatterResolver Utf8JsonResolver = Utf8JsonNS.Resolvers.StandardResolver.Default;

        private static readonly MemoryStream SystemTextJsonOutput = new MemoryStream();
        private static readonly Utf8JsonWriter SystemTextJsonWriter;

        static StructSerializeBenchmark()
        {
            // Hagar
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntStruct>>();
            HagarData = new byte[1000];
            Session = services.GetRequiredService<SessionPool>().GetSession();

            HyperionSession = HyperionSerializer.GetSerializerSession();

            SystemTextJsonWriter = new Utf8JsonWriter(SystemTextJsonOutput);
        }

        [Fact]
        [Benchmark(Baseline = true)]
        public long Hagar()
        {
            Session.FullReset();
            return HagarSerializer.Serialize(Input, HagarData, Session);
        }

        [Benchmark]
        public long Utf8Json()
        {
            Utf8JsonOutput.Position = 0;
            Utf8JsonNS.JsonSerializer.Serialize<IntStruct>(Utf8JsonOutput, Input, Utf8JsonResolver);
            return Utf8JsonOutput.Length;
        }

        [Benchmark]
        public long SystemTextJson()
        {
            SystemTextJsonOutput.Position = 0;
            System.Text.Json.JsonSerializer.Serialize<IntStruct>(SystemTextJsonWriter, Input);

            SystemTextJsonWriter.Reset();
            return SystemTextJsonOutput.Length;
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            var bytes = MessagePack.MessagePackSerializer.Serialize(Input);
            return bytes.Length;
        }

        [Benchmark]
        public long ProtobufNet()
        {
            ProtoBuffer.Position = 0;
            ProtoBuf.Serializer.Serialize(ProtoBuffer, Input);
            return ProtoBuffer.Length;
        }

        [Benchmark]
        public long Hyperion()
        {
            HyperionBuffer.Position = 0;
            HyperionSerializer.Serialize(Input, HyperionBuffer, HyperionSession);
            return HyperionBuffer.Length;
        }

        //[Benchmark]
        public int ZeroFormatter()
        {
            var bytes = ZeroFormatterSerializer.Serialize(Input);
            return bytes.Length;
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Input));
            return bytes.Length;
        }

        [Benchmark(Description = "SpanJson")]
        public int SpanJsonUtf8()
        {
            var bytes = SpanJson.JsonSerializer.Generic.Utf8.Serialize(Input);
            return bytes.Length;
        }
    }
}