using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
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
using ProtoBuf;
using ZeroFormatter;
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks.Comparison
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
        }
    }

    [Serializable]
    [GenerateSerializer]
    [MessagePack.MessagePackObject]
    [ProtoContract]
    [ZeroFormattable]
    public class IntKeySerializerTarget
    {
        [Id(0)]
        [MessagePack.Key(0)]
        [Index(0)]
        [ProtoMember(1)]
        public virtual int MyProperty1 { get; set; }

        [Id(1)]
        [MessagePack.Key(1)]
        [Index(1)]
        [ProtoMember(2)]
        public virtual int MyProperty2 { get; set; }

        [Id(2)]
        [MessagePack.Key(2)]
        [Index(2)]
        [ProtoMember(3)]
        public virtual int MyProperty3 { get; set; }

        [Id(3)]
        [MessagePack.Key(3)]
        [Index(3)]
        [ProtoMember(4)]
        public virtual int MyProperty4 { get; set; }

        [Id(4)]
        [MessagePack.Key(4)]
        [Index(4)]
        [ProtoMember(5)]
        public virtual int MyProperty5 { get; set; }

        [Id(5)]
        [MessagePack.Key(5)]
        [Index(5)]
        [ProtoMember(6)]
        public virtual int MyProperty6 { get; set; }

        [Id(6)]
        [MessagePack.Key(6)]
        [Index(6)]
        [ProtoMember(7)]
        public virtual int MyProperty7 { get; set; }
        
        [Id(7)]
        [ProtoMember(8)]
        [MessagePack.Key(7)]
        [Index(7)]
        public virtual int MyProperty8 { get; set; }

        [Id(8)]
        [ProtoMember(9)]
        [MessagePack.Key(8)]
        [Index(8)]
        public virtual int MyProperty9 { get; set; }
    }
    
    [Config(typeof(BenchmarkConfig))]
    public class DeserializeBenchmark
    {
        private static readonly byte[] IntObj = MessagePack.MessagePackSerializer.Serialize(new IntKeySerializerTarget());
        private static readonly MemoryStream ProtoObj;
        private static readonly string JsonnetObj = JsonConvert.SerializeObject(new IntKeySerializerTarget());
        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] {typeof(IntKeySerializerTarget)}));
        private static readonly MemoryStream HyperionObj;
        private static readonly byte[] ZeroFormatterData = ZeroFormatterSerializer.Serialize(new IntKeySerializerTarget());
        private static readonly Serializer<IntKeySerializerTarget> HagarSerializer;
        private static readonly SingleSegmentBuffer HagarData;
        private static readonly SerializerSession Session;
        private static readonly SerializationManager OrleansSerializer;
        private static readonly List<ArraySegment<byte>> OrleansData;
        private static readonly BinaryTokenStreamReader OrleansBuffer;

        static DeserializeBenchmark()
        {
            ProtoObj = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoObj, new IntKeySerializerTarget());

            HyperionObj = new MemoryStream();
            HyperionSerializer.Serialize(new IntKeySerializerTarget(), HyperionObj);

            // Hagar
            var services = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(Program).Assembly)
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntKeySerializerTarget>>();
            HagarData = new SingleSegmentBuffer();
            var writer = new Writer(HagarData);
            Session = services.GetRequiredService<SessionPool>().GetSession();
            HagarSerializer.Serialize(new IntKeySerializerTarget(),  Session, ref writer);

            // Orleans
            OrleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Configure<SerializationProviderOptions>(options => options.FallbackSerializationProvider = typeof(SupportsNothingSerializer).GetTypeInfo())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();

            var writer2 = new BinaryTokenStreamWriter();
            OrleansSerializer.Serialize(new IntKeySerializerTarget(), writer2);
            OrleansData = writer2.ToBytes();
            OrleansBuffer = new BinaryTokenStreamReader(OrleansData);
        }

        private static int SumResult(IntKeySerializerTarget result)
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
            return SumResult(OrleansSerializer.Deserialize<IntKeySerializerTarget>(OrleansBuffer));
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            return SumResult(MessagePack.MessagePackSerializer.Deserialize<IntKeySerializerTarget>(IntObj));
        }

        [Benchmark]
        public int ProtobufNet()
        {
            ProtoObj.Position = 0;
            return SumResult(ProtoBuf.Serializer.Deserialize<IntKeySerializerTarget>(ProtoObj));
        }

        [Benchmark]
        public int Hyperion()
        {
            HyperionObj.Position = 0;
            return SumResult(HyperionSerializer.Deserialize<IntKeySerializerTarget>(HyperionObj));
        }

        [Benchmark]
        public int ZeroFormatter()
        {
            return SumResult(ZeroFormatterSerializer.Deserialize<IntKeySerializerTarget>(ZeroFormatterData));
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            return SumResult(JsonConvert.DeserializeObject<IntKeySerializerTarget>(JsonnetObj));
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class SerializeBenchmark
    {
        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] { typeof(IntKeySerializerTarget) }));
        private static readonly IntKeySerializerTarget IntData = new IntKeySerializerTarget();
        private static readonly Serializer<IntKeySerializerTarget> HagarSerializer;
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
            HagarSerializer = services.GetRequiredService<Serializer<IntKeySerializerTarget>>();
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

    public class SupportsNothingSerializer : IExternalSerializer
    {
        public bool IsSupportedType(Type itemType) => false;

        public object DeepCopy(object source, ICopyContext context)
        {
            throw new NotSupportedException();
        }

        public void Serialize(object item, ISerializationContext context, Type expectedType)
        {
            throw new NotSupportedException();
        }

        public object Deserialize(Type expectedType, IDeserializationContext context)
        {
            throw new NotSupportedException();
        }
    }
}