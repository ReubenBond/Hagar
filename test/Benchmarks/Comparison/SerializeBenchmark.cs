using System.IO;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
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
using ZeroFormatter;
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks.Comparison
{
    [Config(typeof(BenchmarkConfig))]
    public class SerializeBenchmark
    {
        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] { typeof(MsgPackInts) }));
        private static readonly MsgPackInts IntData = new MsgPackInts();
        private static readonly Serializer<MsgPackInts> HagarSerializer;
        private static readonly SingleSegmentBuffer HagarData;
        private static readonly SerializerSession Session;
        private static readonly SerializationManager OrleansSerializer;
        private static readonly MemoryStream ProtoBuffer = new MemoryStream();
        private static readonly MemoryStream HyperionBuffer = new MemoryStream();

        static SerializeBenchmark()
        {
            // Hagar
            var services = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(Program).Assembly)
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<MsgPackInts>>();
            HagarData = new SingleSegmentBuffer();
            Session = services.GetRequiredService<SessionPool>().GetSession();

            // Orleans
            OrleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Configure<SerializationProviderOptions>(options => options.FallbackSerializationProvider = typeof(SupportsNothingSerializer).GetTypeInfo())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();
        }

        [Benchmark(Baseline = true)]
        public long Hagar()
        {
            HagarData.Reset();
            Session.FullReset();
            var writer = new Writer(HagarData);
            HagarSerializer.Serialize(IntData, Session, ref writer);
            return HagarData.Length;
        }

        [Benchmark]
        public int Orleans()
        {
            var orleansBuffer = new BinaryTokenStreamWriter();
            OrleansSerializer.Serialize(IntData, orleansBuffer);
            var result = orleansBuffer.CurrentOffset;
            orleansBuffer.ReleaseBuffers();
            return result;
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            var bytes = MessagePack.MessagePackSerializer.Serialize(IntData);
            return bytes.Length;
        }

        [Benchmark]
        public long ProtobufNet()
        {
            ProtoBuffer.Position = 0;
            ProtoBuf.Serializer.Serialize(ProtoBuffer, IntData);
            return ProtoBuffer.Position;
        }

        [Benchmark]
        public long Hyperion()
        {
            HyperionBuffer.Position = 0;
            HyperionSerializer.Serialize(IntData, HyperionBuffer);
            return HyperionBuffer.Position;
        }

        [Benchmark]
        public int ZeroFormatter()
        {
            var bytes = ZeroFormatterSerializer.Serialize(IntData);
            return bytes.Length;
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(IntData));
            return bytes.Length;
        }
    }
}