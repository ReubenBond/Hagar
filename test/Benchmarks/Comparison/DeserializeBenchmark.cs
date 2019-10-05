using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Xunit;
using ZeroFormatter;
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    public class DeserializeBenchmark
    {
        private static readonly MemoryStream ProtoInput;

        private static readonly byte[] MsgPackInput = MessagePack.MessagePackSerializer.Serialize(IntClass.Create());

        private static readonly string NewtonsoftJsonInput = JsonConvert.SerializeObject(IntClass.Create());

        private static readonly byte[] SpanJsonInput = SpanJson.JsonSerializer.Generic.Utf8.Serialize(IntClass.Create());

        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] {typeof(IntClass) }));
        private static readonly MemoryStream HyperionInput;

        private static readonly Serializer<IntClass> HagarSerializer;
        private static readonly byte[] HagarInput;
        private static readonly SerializerSession Session;

        private static readonly SerializationManager OrleansSerializer;
        private static readonly List<ArraySegment<byte>> OrleansInput;
        private static readonly BinaryTokenStreamReader OrleansBuffer;

        static DeserializeBenchmark()
        {
            ProtoInput = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoInput, IntClass.Create());

            HyperionInput = new MemoryStream();
            HyperionSerializer.Serialize(IntClass.Create(), HyperionInput);

            // Hagar
            var services = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(Program).Assembly)
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntClass>>();
            var bytes = new byte[1000];
            Session = services.GetRequiredService<SessionPool>().GetSession();
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            HagarSerializer.Serialize(ref writer, IntClass.Create());
            HagarInput = bytes;

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

            var writer2 = new BinaryTokenStreamWriter();
            OrleansSerializer.Serialize(IntClass.Create(), writer2);
            OrleansInput = writer2.ToBytes();
            OrleansBuffer = new BinaryTokenStreamReader(OrleansInput);
        }

        private static int SumResult(IntClass result)
        {
            return result.MyProperty1 +
                   result.MyProperty2 +
                   result.MyProperty3 +
                   result.MyProperty4 +
                   result.MyProperty5 +
                   result.MyProperty6 +
                   result.MyProperty7 +
                   result.MyProperty8 +
                   result.MyProperty9;
        }

        private static int SumResult(VirtualIntsClass result)
        {
            return result.MyProperty1 +
                   result.MyProperty2 +
                   result.MyProperty3 +
                   result.MyProperty4 +
                   result.MyProperty5 +
                   result.MyProperty6 +
                   result.MyProperty7 +
                   result.MyProperty8 +
                   result.MyProperty9;
        }

        [Fact]
        [Benchmark(Baseline = true)]
        public int Hagar()
        {
            Session.FullReset();
            var reader = new Reader(new ReadOnlySequence<byte>(HagarInput), Session);
            return SumResult(HagarSerializer.Deserialize(ref reader));
        }

        [Benchmark]
        public int Orleans()
        {
            OrleansBuffer.Reset(OrleansInput);
            return SumResult(OrleansSerializer.Deserialize<IntClass>(OrleansBuffer));
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            return SumResult(MessagePack.MessagePackSerializer.Deserialize<IntClass>(MsgPackInput));
        }

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
            return SumResult(HyperionSerializer.Deserialize<IntClass>(HyperionInput));
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            return SumResult(JsonConvert.DeserializeObject<IntClass>(NewtonsoftJsonInput));
        }

        [Benchmark(Description = "SpanJson")]
        public int SpanJsonUtf8()
        {
            return SumResult(SpanJson.JsonSerializer.Generic.Utf8.Deserialize<IntClass>(SpanJsonInput));
        }
    }
}