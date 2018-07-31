using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    public class StructDeserializeBenchmark
    {
        private static readonly byte[] IntObj = MessagePack.MessagePackSerializer.Serialize(new IntStruct());
        private static readonly MemoryStream ProtoObj;
        private static readonly string JsonnetObj = JsonConvert.SerializeObject(new IntStruct());
        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] {typeof(IntStruct)}));
        private static readonly MemoryStream HyperionObj;
        private static readonly byte[] ZeroFormatterData = ZeroFormatterSerializer.Serialize(new IntStruct());
        private static readonly Serializer<IntStruct> HagarSerializer;
        private static readonly SingleSegmentBuffer HagarData;
        private static readonly SerializerSession Session;
        private static readonly SerializationManager OrleansSerializer;
        private static readonly List<ArraySegment<byte>> OrleansData;
        private static readonly BinaryTokenStreamReader OrleansBuffer;

        static StructDeserializeBenchmark()
        {
            ProtoObj = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoObj, new IntStruct());

            HyperionObj = new MemoryStream();
            HyperionSerializer.Serialize(new IntStruct(), HyperionObj);

            // Hagar
            var services = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(Program).Assembly)
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntStruct>>();
            HagarData = new SingleSegmentBuffer();
            var writer = new Writer(HagarData);
            Session = services.GetRequiredService<SessionPool>().GetSession();
            HagarSerializer.Serialize(new IntStruct(),  Session, ref writer);

            // Orleans
            OrleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Configure<SerializationProviderOptions>(options => options.FallbackSerializationProvider = typeof(SupportsNothingSerializer).GetTypeInfo())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();

            var writer2 = new BinaryTokenStreamWriter();
            OrleansSerializer.Serialize(new IntStruct(), writer2);
            OrleansData = writer2.ToBytes();
            OrleansBuffer = new BinaryTokenStreamReader(OrleansData);
        }

        private static int SumResult(IntStruct result)
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

        [Benchmark(Baseline = true)]
        public int Hagar()
        {
            Session.FullReset();
            var reader = new Reader(HagarData.GetReadOnlySequence());
            return SumResult(HagarSerializer.Deserialize(Session, ref reader));
        }

        [Benchmark]
        public int Orleans()
        {
            OrleansBuffer.Reset(OrleansData);
            return SumResult(OrleansSerializer.Deserialize<IntStruct>(OrleansBuffer));
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            return SumResult(MessagePack.MessagePackSerializer.Deserialize<IntStruct>(IntObj));
        }

        [Benchmark]
        public int ProtobufNet()
        {
            ProtoObj.Position = 0;
            return SumResult(ProtoBuf.Serializer.Deserialize<IntStruct>(ProtoObj));
        }

        [Benchmark]
        public int Hyperion()
        {
            HyperionObj.Position = 0;
            return SumResult(HyperionSerializer.Deserialize<IntStruct>(HyperionObj));
        }

        [Benchmark]
        public int ZeroFormatter()
        {
            return SumResult(ZeroFormatterSerializer.Deserialize<IntStruct>(ZeroFormatterData));
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            return SumResult(JsonConvert.DeserializeObject<IntStruct>(JsonnetObj));
        }
    }
}