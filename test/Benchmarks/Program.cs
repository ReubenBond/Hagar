using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Serialization;
using Orleans.Hosting;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //new ComplexTypeBenchmarks().HagarSerializer();
            BenchmarkRunner.Run<ComplexTypeBenchmarks>();
        }
    }
    
    [MemoryDiagnoser]
    public class ComplexTypeBenchmarks
    {
        private readonly Serializer<ComplexClass> hagarSerializer;
        private readonly SessionPool sessionPool;
        private readonly ComplexClass value;
        private readonly SerializationManager orleansSerializer;
        private readonly SerializerSession session;
        private List<ArraySegment<byte>> hagarBytes;
        private List<ArraySegment<byte>> orleansBytes;

        public ComplexTypeBenchmarks()
        {
            this.orleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();
            var services = new ServiceCollection();
            services
                .AddHagar()
                .AddISerializableSupport()
                .AddSerializers(typeof(Program).Assembly);
            var serviceProvider = services.BuildServiceProvider();
            this.hagarSerializer = serviceProvider.GetRequiredService<Serializer<ComplexClass>>();
            this.sessionPool = serviceProvider.GetRequiredService<SessionPool>();
            this.value = new ComplexClass
            {
                BaseInt = 192,
                Int = 501,
                String = "bananas",
                Array = Enumerable.Range(0, 60).ToArray(),
                MultiDimensionalArray = new[,] {{0, 2, 4}, {1, 5, 6}}
            };
            this.value.AlsoSelf = this.value.BaseSelf = this.value.Self = this.value;
            this.session = sessionPool.GetSession();
            var writer = new Writer();
            this.hagarSerializer.Serialize(this.value, session, writer);
            this.hagarBytes = writer.ToBytes();
            
            var writer2 = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer2);
            this.orleansBytes = writer2.ToBytes();
        }

        //[Benchmark]
        public object HagarSerializer()
        {
            var writer = new Writer();
            session.FullReset();
            this.hagarSerializer.Serialize(this.value, session, writer);

            session.FullReset();

            var reader = new Reader(writer.ToBytes());
            return this.hagarSerializer.Deserialize(session, reader);
        }

        //[Benchmark]
        public object OrleansSerializer()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer);
            return this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(writer.ToBytes()));
        }

        [Benchmark]
        public object HagarSerialize()
        {
            var writer = new Writer();
            session.FullReset();
            this.hagarSerializer.Serialize(this.value, session, writer);
            return session;
        }

        [Benchmark]
        public object OrleansSerialize()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer);
            return writer;
        }

        [Benchmark]
        public object HagarDeserialize()
        {
            session.FullReset();
            return this.hagarSerializer.Deserialize(session, new Reader(this.hagarBytes));
        }

        [Benchmark]
        public object OrleansDeserialize()
        {
            return this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(this.orleansBytes));
        }
    }

    [Serializable]
    [GenerateSerializer]
    public class SimpleClass
    {
        [FieldId(0)]
        public int BaseInt { get; set; }
    }

    [Serializable]
    [GenerateSerializer]
    public class ComplexClass : SimpleClass
    {
        [FieldId(0)]
        public int Int { get; set; }

        [FieldId(1)]
        public string String { get; set; }

        [FieldId(2)]
        public ComplexClass Self { get; set; }

        [FieldId(3)]
        public object AlsoSelf { get; set; }

        [FieldId(4)]
        public SimpleClass BaseSelf { get; set; }

        [FieldId(5)]
        public int[] Array { get; set; }

        [FieldId(6)]
        public int[,] MultiDimensionalArray { get; set; }
    }
}
