using System.IO;
using System.Reflection;
using System.Text;
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
using ZeroFormatter;
using Xunit;
using SerializerSession = Hagar.Session.SerializerSession;
using System.Linq;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    [PayloadSizeColumn]
    public class SerializeBenchmark
    {
        private static readonly IntClass Input = IntClass.Create();
        private static readonly VirtualIntsClass ZeroFormatterInput = VirtualIntsClass.Create();

        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] { typeof(IntClass) }));
        private static readonly Serializer<IntClass> HagarSerializer;
        private static readonly byte[] HagarData;
        private static readonly SerializerSession Session;
        private static readonly SerializationManager OrleansSerializer;
        private static readonly MemoryStream ProtoBuffer = new MemoryStream();
        private static readonly MemoryStream HyperionBuffer = new MemoryStream();

        static SerializeBenchmark()
        {
            // Hagar
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntClass>>();
            Session = services.GetRequiredService<SessionPool>().GetSession();
            HagarData = new byte[1000];

            // Orleans
            OrleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .ConfigureServices(s => s.ToList().ForEach(r =>
                {
                    if (r.ServiceType == typeof(IConfigurationValidator)) s.Remove(r);
                }))
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Configure<SerializationProviderOptions>(options => options.FallbackSerializationProvider = typeof(SupportsNothingSerializer).GetTypeInfo())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();
        }

        [Fact]
        [Benchmark(Baseline = true)]
        public long Hagar()
        {
            Session.PartialReset();
            var writer = new SingleSegmentBuffer(HagarData).CreateWriter(Session);
            HagarSerializer.Serialize(ref writer, Input);
            return writer.Output.Length;
        }

        //[Benchmark]
        public int Orleans()
        {
            var orleansBuffer = new BinaryTokenStreamWriter();
            OrleansSerializer.Serialize(Input, orleansBuffer);
            var result = orleansBuffer.CurrentOffset;
            orleansBuffer.ReleaseBuffers();
            return result;
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
            HyperionSerializer.Serialize(Input, HyperionBuffer);
            return HyperionBuffer.Length;
        }

        [Benchmark]
        public int ZeroFormatter()
        {
            var bytes = ZeroFormatterSerializer.Serialize(ZeroFormatterInput);
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