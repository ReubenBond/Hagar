using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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
using ZeroFormatter;
using Xunit;
using SerializerSession = Hagar.Session.SerializerSession;
using System.Linq;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    public class StructDeserializeBenchmark
    {
        private static readonly MemoryStream ProtoInput;
        private static readonly string NewtonsoftJsonInput = JsonConvert.SerializeObject(IntStruct.Create());

        private static readonly byte[] SpanJsonInput = SpanJson.JsonSerializer.Generic.Utf8.Serialize(IntStruct.Create());

        private static readonly byte[] MsgPackInput = MessagePack.MessagePackSerializer.Serialize(IntStruct.Create());
        private static readonly byte[] ZeroFormatterInput = ZeroFormatterSerializer.Serialize(IntStruct.Create());
        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] {typeof(IntStruct)}));
        private static readonly MemoryStream HyperionInput;
        private static readonly ValueSerializer<IntStruct> HagarSerializer;
        private static readonly ReadOnlySequence<byte> HagarInput;
        private static readonly SerializerSession Session;
        private static readonly SerializationManager OrleansSerializer;
        private static readonly List<ArraySegment<byte>> OrleansInput;
        private static readonly BinaryTokenStreamReader OrleansBuffer;

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
            HagarSerializer.Serialize(ref writer, IntStruct.Create());
            HagarInput = new ReadOnlySequence<byte>(bytes);

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
            OrleansSerializer.Serialize(IntStruct.Create(), writer2);
            OrleansInput = writer2.ToBytes();
            OrleansBuffer = new BinaryTokenStreamReader(OrleansInput);
        }

        private static int SumResult(in IntStruct result)
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
            var reader = new Reader(HagarInput, Session);
            IntStruct result = default;
            HagarSerializer.Deserialize(ref reader, ref result);
            return SumResult(in result);
        }

        //[Benchmark]
        public int Orleans()
        {
            OrleansBuffer.Reset(OrleansInput);
            return SumResult(OrleansSerializer.Deserialize<IntStruct>(OrleansBuffer));
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            return SumResult(MessagePack.MessagePackSerializer.Deserialize<IntStruct>(MsgPackInput));
        }

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
            return SumResult(HyperionSerializer.Deserialize<IntStruct>(HyperionInput));
        }

        [Benchmark]
        public int ZeroFormatter()
        {
            return SumResult(ZeroFormatterSerializer.Deserialize<IntStruct>(ZeroFormatterInput));
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            return SumResult(JsonConvert.DeserializeObject<IntStruct>(NewtonsoftJsonInput));
        }

        [Benchmark(Description = "SpanJson")]
        public int SpanJsonUtf8()
        {
            return SumResult(SpanJson.JsonSerializer.Generic.Utf8.Deserialize<IntStruct>(SpanJsonInput));
        }
    }
}