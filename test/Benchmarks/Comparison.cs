using System.Buffers;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProtoBuf;
using ZeroFormatter;

namespace Benchmarks
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
        }
    }

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
        static byte[] intObj = MessagePack.MessagePackSerializer.Serialize(new IntKeySerializerTarget());
        static byte[] protoObj;
        static string jsonnetObj = JsonConvert.SerializeObject(new IntKeySerializerTarget());
        static Hyperion.Serializer hyperionSerializer = new Hyperion.Serializer();
        static byte[] hyperionObj;
        private static byte[] zeroFormatterData = ZeroFormatterSerializer.Serialize(new IntKeySerializerTarget());
        private static Serializer<IntKeySerializerTarget> hagarSerializer;
        private static SingleSegmentBuffer hagarData;
        private static SerializerSession session;

        static DeserializeBenchmark()
        {
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, new IntKeySerializerTarget());
                protoObj = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                hyperionSerializer.Serialize(new IntKeySerializerTarget(), ms);
                hyperionObj = ms.ToArray();
            }

            var services = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(Program).Assembly)
                .BuildServiceProvider();
            hagarSerializer = services.GetRequiredService<Hagar.Serializer<IntKeySerializerTarget>>();
            hagarData = new SingleSegmentBuffer();
            var writer = new Writer(hagarData);
            session = services.GetRequiredService<SessionPool>().GetSession();
            hagarSerializer.Serialize(new IntKeySerializerTarget(),  session, ref writer);
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
            session.FullReset();
            var reader = new Reader(hagarData.GetReadOnlySequence());
            return SumResult(hagarSerializer.Deserialize(session, ref reader));
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            return SumResult(MessagePack.MessagePackSerializer.Deserialize<IntKeySerializerTarget>(intObj));
        }

        [Benchmark]
        public int ProtobufNet()
        {
            using (var ms = new MemoryStream(protoObj))
            {
                return SumResult(ProtoBuf.Serializer.Deserialize<IntKeySerializerTarget>(ms));
            }
        }

        [Benchmark]
        public int Hyperion()
        {
            using (var ms = new MemoryStream(hyperionObj))
            {
                return SumResult(hyperionSerializer.Deserialize<IntKeySerializerTarget>(ms));
            }
        }

        [Benchmark]
        public int ZeroFormatter()
        {
            return SumResult(ZeroFormatterSerializer.Deserialize<IntKeySerializerTarget>(zeroFormatterData));
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            return SumResult(JsonConvert.DeserializeObject<IntKeySerializerTarget>(jsonnetObj));
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class SerializeBenchmark
    {
        static Hyperion.Serializer hyperionSerializer = new Hyperion.Serializer();
        static IntKeySerializerTarget intData = new IntKeySerializerTarget();
        private static Serializer<IntKeySerializerTarget> hagarSerializer;
        private static SingleSegmentBuffer hagarData;
        private static SerializerSession session;

        static SerializeBenchmark()
        {

            var services = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(Program).Assembly)
                .BuildServiceProvider();
            hagarSerializer = services.GetRequiredService<Hagar.Serializer<IntKeySerializerTarget>>();
            hagarData = new SingleSegmentBuffer();
            session = services.GetRequiredService<SessionPool>().GetSession();
        }

        [Benchmark(Baseline = true)]
        public byte[] Hagar()
        {
            hagarData.Reset();
            var writer = new Writer(hagarData);
            hagarSerializer.Serialize(intData, session, ref writer);
            return hagarData.ToArray();
        }

        [Benchmark]
        public byte[] MessagePackCSharp()
        {
            return MessagePack.MessagePackSerializer.Serialize<IntKeySerializerTarget>(intData);
        }

        [Benchmark]
        public byte[] ProtobufNet()
        {
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<IntKeySerializerTarget>(ms, intData);
                return ms.ToArray();
            }
        }

        [Benchmark]
        public byte[] Hyperion()
        {
            using (var ms = new MemoryStream())
            {
                hyperionSerializer.Serialize(intData, ms);
                return ms.ToArray();
            }
        }

        [Benchmark]
        public byte[] ZeroFormatter()
        {
            return ZeroFormatterSerializer.Serialize(intData);
        }

        [Benchmark]
        public byte[] NewtonsoftJson()
        {
            return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(intData));
        }
    }
}