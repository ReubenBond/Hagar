using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
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
            if (args.Length > 0 && args[0] == "loop")
            {
                var benchmarks = new ComplexTypeBenchmarks();
                while (true)
                {
                    benchmarks.SerializeComplex();
                }
            }

            if (args.Length > 0 && args[0] == "structloop")
            {
                var benchmarks = new ComplexTypeBenchmarks();
                while (true)
                {
                    benchmarks.SerializeStruct();
                }
            }

            BenchmarkRunner.Run<ComplexTypeBenchmarks>();
        }
    }
    
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

        [Benchmark]
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

        [Benchmark]
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

        [Benchmark]
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

    [Serializable]
    [GenerateSerializer]
    public class SimpleClass
    {
        [Id(0)]
        public int BaseInt { get; set; }
    }

    [Serializable]
    [GenerateSerializer]
    public class ComplexClass : SimpleClass
    {
        [Id(0)]
        public int Int { get; set; }

        [Id(1)]
        public string String { get; set; }

        [Id(2)]
        public ComplexClass Self { get; set; }

        [Id(3)]
        public object AlsoSelf { get; set; }

        [Id(4)]
        public SimpleClass BaseSelf { get; set; }

        [Id(5)]
        public int[] Array { get; set; }

        [Id(6)]
        public int[,] MultiDimensionalArray { get; set; }
    }

    [Serializable]
    [GenerateSerializer]
    public struct SimpleStruct
    {
        [Id(0)]
        public int Int { get; set; }

        [Id(1)]
        public bool Bool { get; set; }
        
        [Id(3)]
        public object AlwaysNull { get; set; }

        [Id(4)]
        public Guid Guid { get; set; }
    }

    public class SingleSegmentBuffer : IBufferWriter<byte>
    {
        private readonly byte[] buffer = new byte[1000];
        private int written;

        public void Advance(int bytes)
        {
            written += bytes;
        }

        public Memory<byte> GetMemory(int sizeHint = 0) => buffer.AsMemory().Slice(written);

        public Span<byte> GetSpan(int sizeHint) => buffer.AsSpan().Slice(written);

        public void Reset() => this.written = 0;

        public ReadOnlySequence<byte> GetReadOnlySequence() => new ReadOnlySequence<byte>(this.buffer, 0, this.written);

        public override string ToString()
        {
            return Encoding.UTF8.GetString(buffer.AsSpan(0, written).ToArray());
        }
    }
}
