using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ComplexTypeBenchmarks
    {
        private readonly Serializer<SimpleStruct> structSerializer;
        private readonly Serializer<ComplexClass> hagarSerializer;
        private readonly SessionPool sessionPool;
        private readonly ComplexClass value;
        private readonly SerializationManager orleansSerializer;
        private readonly SerializerSession session;
        private readonly ReadOnlySequence<byte> hagarBytes;
        private readonly List<ArraySegment<byte>> orleansBytes;
        private readonly long readBytesLength;
        private SimpleStruct structValue;

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
            this.structSerializer = serviceProvider.GetRequiredService<Serializer<SimpleStruct>>();
            this.sessionPool = serviceProvider.GetRequiredService<SessionPool>();
            this.value = new ComplexClass
            {
                BaseInt = 192,
                Int = 501,
                String = "bananas",
                //Array = Enumerable.Range(0, 60).ToArray(),
                //MultiDimensionalArray = new[,] {{0, 2, 4}, {1, 5, 6}}
            };
            this.value.AlsoSelf = this.value.BaseSelf = this.value.Self = this.value;

            this.structValue = new SimpleStruct
            {
                Int = 42,
                Bool = true,
                Guid = Guid.NewGuid()
            };
            this.session = sessionPool.GetSession();
            var writer = new Writer(HagarBuffer);
            this.hagarSerializer.Serialize(this.value, session, ref writer);
            var bytes = new byte[HagarBuffer.GetMemory().Length];
            HagarBuffer.GetReadOnlySequence().CopyTo(bytes);
            this.hagarBytes = new ReadOnlySequence<byte>(bytes);
            HagarBuffer.Reset();

            var writer2 = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer2);
            this.orleansBytes = writer2.ToBytes();

            this.readBytesLength = Math.Min(bytes.Length, orleansBytes.Sum(x => x.Count));
        }

        public void SerializeComplex()
        {
            var writer = new Writer(HagarBuffer);
            session.FullReset();
            this.hagarSerializer.Serialize(this.value, session, ref writer);

            session.FullReset();
            var reader = new Reader(new ReadOnlySequence<byte>(HagarBuffer.GetMemory()));
            this.hagarSerializer.Deserialize(session, ref reader);
            HagarBuffer.Reset();
        }

        public void SerializeStruct()
        {
            var writer = new Writer(HagarBuffer);
            session.FullReset();
            this.structSerializer.Serialize(this.structValue, session, ref writer);

            session.FullReset();
            var reader = new Reader(HagarBuffer.GetReadOnlySequence());
            this.structSerializer.Deserialize(session, ref reader);
            HagarBuffer.Reset();
        }
        
        private static readonly SingleSegmentBuffer HagarBuffer = new SingleSegmentBuffer();

        [Benchmark]
        public SimpleStruct HagarStructRoundTrip()
        {
            var writer = new Writer(HagarBuffer);
            session.FullReset();
            this.structSerializer.Serialize(this.structValue, session, ref writer);

            session.FullReset();
            var reader = new Reader(HagarBuffer.GetReadOnlySequence());
            var result = this.structSerializer.Deserialize(session, ref reader);
            HagarBuffer.Reset();
            return result;
        }

        //[Benchmark]
        public SimpleStruct OrleansStructRoundTrip()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.structValue, writer);
            return (SimpleStruct)this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(writer.ToBytes()));
        }

        //[Benchmark]
        public object HagarClassRoundTrip()
        {
            var writer = new Writer(HagarBuffer);
            session.FullReset();
            this.hagarSerializer.Serialize(this.value, session, ref writer);

            session.FullReset();
            var reader = new Reader(HagarBuffer.GetReadOnlySequence());
            var result = this.hagarSerializer.Deserialize(session, ref reader);
            HagarBuffer.Reset();
            return result;
        }

        //[Benchmark]
        public object OrleansClassRoundTrip()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer);
            return this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(writer.ToBytes()));
        }

        //[Benchmark]
        public object HagarSerialize()
        {
            var writer = new Writer(HagarBuffer);
            session.FullReset();
            this.hagarSerializer.Serialize(this.value, session, ref writer);
            HagarBuffer.Reset();
            return session;
        }

        //[Benchmark]
        public object OrleansSerialize()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer);
            return writer;
        }

        //[Benchmark]
        public object HagarDeserialize()
        {
            session.FullReset();
            var reader = new Reader(this.hagarBytes);
            return this.hagarSerializer.Deserialize(session, ref reader);
        }

        //[Benchmark]
        public object OrleansDeserialize()
        {
            return this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(this.orleansBytes));
        }

        //[Benchmark]
        public int HagarReadEachByte()
        {
            var sum = 0;
            var reader = new Reader(this.hagarBytes);
            for (var i = 0; i < readBytesLength; i++) sum ^= reader.ReadByte();
            return sum;
        }

        //[Benchmark]
        public int OrleansReadEachByte()
        {
            var sum = 0;
            var reader = new BinaryTokenStreamReader(this.orleansBytes);
            for (var i = 0; i < readBytesLength; i++) sum ^= reader.ReadByte();
            return sum;
        }
    }
}