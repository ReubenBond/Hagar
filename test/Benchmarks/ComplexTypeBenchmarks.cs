using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks.Models;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using Xunit;

namespace Benchmarks
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class ComplexTypeBenchmarks
    {
        private static SingleSegmentBuffer hagarBuffer = new SingleSegmentBuffer(new byte[1000]);
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
            var writer = hagarBuffer.CreateWriter(this.session);
            this.hagarSerializer.Serialize(ref writer, this.value);
            var bytes = new byte[writer.Output.GetMemory().Length];
            writer.Output.GetReadOnlySequence().CopyTo(bytes);
            this.hagarBytes = new ReadOnlySequence<byte>(bytes);
            hagarBuffer.Reset();

            var writer2 = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer2);
            this.orleansBytes = writer2.ToBytes();

            this.readBytesLength = Math.Min(bytes.Length, orleansBytes.Sum(x => x.Count));
        }

        [Fact]
        public void SerializeComplex()
        {
            var writer = hagarBuffer.CreateWriter(this.session);
            session.FullReset();
            this.hagarSerializer.Serialize(ref writer, this.value);

            session.FullReset();
            var reader = new Reader(writer.Output.GetReadOnlySequence(), session);
            this.hagarSerializer.Deserialize(ref reader);
            hagarBuffer.Reset();
        }
        
        [Fact]
        [Benchmark]
        public SimpleStruct HagarStructRoundTrip()
        {
            var writer = hagarBuffer.CreateWriter(this.session);
            this.session.FullReset();
            this.structSerializer.Serialize(ref writer, this.structValue);

            this.session.FullReset();
            var reader = new Reader(writer.Output.GetReadOnlySequence(), this.session);
            var result = this.structSerializer.Deserialize(ref reader);
            hagarBuffer.Reset();
            return result;
        }

        //[Benchmark]
        public SimpleStruct OrleansStructRoundTrip()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.structValue, writer);
            return (SimpleStruct)this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(writer.ToBytes()));
        }

        [Fact]
        //[Benchmark]
        public object HagarClassRoundTrip()
        {
            var writer = hagarBuffer.CreateWriter(this.session);
            this.session.FullReset();
            this.hagarSerializer.Serialize(ref writer, this.value);

            this.session.FullReset();
            var reader = new Reader(writer.Output.GetReadOnlySequence(), this.session);
            var result = this.hagarSerializer.Deserialize(ref reader);
            hagarBuffer.Reset();
            return result;
        }

        //[Benchmark]
        public object OrleansClassRoundTrip()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer);
            return this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(writer.ToBytes()));
        }

        [Fact]
        //[Benchmark]
        public object HagarSerialize()
        {
            var writer = hagarBuffer.CreateWriter(this.session);
            session.FullReset();
            this.hagarSerializer.Serialize(ref writer, this.value);
            hagarBuffer.Reset();
            return session;
        }

        //[Benchmark]
        public object OrleansSerialize()
        {
            var writer = new BinaryTokenStreamWriter();
            this.orleansSerializer.Serialize(this.value, writer);
            return writer;
        }

        [Fact]
        //[Benchmark]
        public object HagarDeserialize()
        {
            this.session.FullReset();
            var reader = new Reader(this.hagarBytes, this.session);
            return this.hagarSerializer.Deserialize(ref reader);
        }

        //[Benchmark]
        public object OrleansDeserialize()
        {
            return this.orleansSerializer.Deserialize(new BinaryTokenStreamReader(this.orleansBytes));
        }

        [Fact]
        //[Benchmark]
        public int HagarReadEachByte()
        {
            var sum = 0;
            var reader = new Reader(this.hagarBytes, session);
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