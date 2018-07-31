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
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks.Comparison
{
    [Config(typeof(BenchmarkConfig))]
    public class DeserializeBenchmark
    {
        private static readonly MemoryStream ProtoObj;
        private static readonly string JsonnetObj = JsonConvert.SerializeObject(new MsgPackInts());
        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] {typeof(MsgPackInts)}));
        private static readonly MemoryStream HyperionObj;
        private static readonly Serializer<MsgPackInts> HagarSerializer;
        private static readonly SingleSegmentBuffer HagarData;
        private static readonly SerializerSession Session;
        private static readonly SerializationManager OrleansSerializer;
        private static readonly List<ArraySegment<byte>> OrleansData;
        private static readonly BinaryTokenStreamReader OrleansBuffer;

        static DeserializeBenchmark()
        {
            ProtoObj = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoObj, new MsgPackInts());

            HyperionObj = new MemoryStream();
            HyperionSerializer.Serialize(new MsgPackInts(), HyperionObj);

            // Hagar
            var services = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(Program).Assembly)
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<MsgPackInts>>();
            HagarData = new SingleSegmentBuffer();
            var writer = new Writer(HagarData);
            Session = services.GetRequiredService<SessionPool>().GetSession();
            HagarSerializer.Serialize(new MsgPackInts(),  Session, ref writer);

            // Orleans
            OrleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Configure<SerializationProviderOptions>(options => options.FallbackSerializationProvider = typeof(SupportsNothingSerializer).GetTypeInfo())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();

            var writer2 = new BinaryTokenStreamWriter();
            OrleansSerializer.Serialize(new MsgPackInts(), writer2);
            OrleansData = writer2.ToBytes();
            OrleansBuffer = new BinaryTokenStreamReader(OrleansData);
        }

        private static int SumResult(MsgPackInts result)
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
            return SumResult(OrleansSerializer.Deserialize<MsgPackInts>(OrleansBuffer));
        }

        [Benchmark]
        public int ProtobufNet()
        {
            ProtoObj.Position = 0;
            return SumResult(ProtoBuf.Serializer.Deserialize<MsgPackInts>(ProtoObj));
        }

        [Benchmark]
        public int Hyperion()
        {
            HyperionObj.Position = 0;
            return SumResult(HyperionSerializer.Deserialize<MsgPackInts>(HyperionObj));
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            return SumResult(JsonConvert.DeserializeObject<MsgPackInts>(JsonnetObj));
        }
    }
}