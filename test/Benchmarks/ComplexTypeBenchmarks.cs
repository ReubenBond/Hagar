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
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Benchmarks
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class ComplexTypeBenchmarks
    {
        private static SingleSegmentBuffer HagarBuffer = new(new byte[1000]);
        private readonly Serializer<SimpleStruct> _structSerializer;
        private readonly Serializer<ComplexClass> _hagarSerializer;
        private readonly SerializerSessionPool _sessionPool;
        private readonly ComplexClass _value;
        private readonly SerializationManager _orleansSerializer;
        private readonly SerializerSession _session;
        private readonly ReadOnlySequence<byte> _hagarBytes;
        private readonly List<ArraySegment<byte>> _orleansBytes;
        private readonly long _readBytesLength;
        private SimpleStruct _structValue;

        public ComplexTypeBenchmarks()
        {
            _orleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .ConfigureServices(s => s.ToList().ForEach(r =>
                {
                    if (r.ServiceType == typeof(IConfigurationValidator))
                    {
                        _ = s.Remove(r);
                    }
                }))
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();
            var services = new ServiceCollection();
            _ = services
                .AddHagar();
            var serviceProvider = services.BuildServiceProvider();
            _hagarSerializer = serviceProvider.GetRequiredService<Serializer<ComplexClass>>();
            _structSerializer = serviceProvider.GetRequiredService<Serializer<SimpleStruct>>();
            _sessionPool = serviceProvider.GetRequiredService<SerializerSessionPool>();
            _value = new ComplexClass
            {
                BaseInt = 192,
                Int = 501,
                String = "bananas",
                //Array = Enumerable.Range(0, 60).ToArray(),
                //MultiDimensionalArray = new[,] {{0, 2, 4}, {1, 5, 6}}
            };
            _value.AlsoSelf = _value.BaseSelf = _value.Self = _value;

            _structValue = new SimpleStruct
            {
                Int = 42,
                Bool = true,
                Guid = Guid.NewGuid()
            };
            _session = _sessionPool.GetSession();
            var writer = HagarBuffer.CreateWriter(_session);

            _hagarSerializer.Serialize(_value, ref writer);
            var bytes = new byte[writer.Output.GetMemory().Length];
            writer.Output.GetReadOnlySequence().CopyTo(bytes);
            _hagarBytes = new ReadOnlySequence<byte>(bytes);
            HagarBuffer.Reset();

            var writer2 = new BinaryTokenStreamWriter();
            _orleansSerializer.Serialize(_value, writer2);
            _orleansBytes = writer2.ToBytes();

            _readBytesLength = Math.Min(bytes.Length, _orleansBytes.Sum(x => x.Count));
        }

        [Fact]
        public void SerializeComplex()
        {
            var writer = HagarBuffer.CreateWriter(_session);
            _session.FullReset();
            _hagarSerializer.Serialize(_value, ref writer);

            _session.FullReset();
            var reader = Reader.Create(writer.Output.GetReadOnlySequence(), _session);
            _ = _hagarSerializer.Deserialize(ref reader);
            HagarBuffer.Reset();
        }

        [Fact]
        [Benchmark]
        public SimpleStruct HagarStructRoundTrip()
        {
            var writer = HagarBuffer.CreateWriter(_session);
            _session.FullReset();
            _structSerializer.Serialize(_structValue, ref writer);

            _session.FullReset();
            var reader = Reader.Create(writer.Output.GetReadOnlySequence(), _session);
            var result = _structSerializer.Deserialize(ref reader);
            HagarBuffer.Reset();
            return result;
        }

        //[Benchmark]
        public SimpleStruct OrleansStructRoundTrip()
        {
            var writer = new BinaryTokenStreamWriter();
            _orleansSerializer.Serialize(_structValue, writer);
            return (SimpleStruct)_orleansSerializer.Deserialize(new BinaryTokenStreamReader(writer.ToBytes()));
        }

        [Fact]
        //[Benchmark]
        public object HagarClassRoundTrip()
        {
            var writer = HagarBuffer.CreateWriter(_session);
            _session.FullReset();
            _hagarSerializer.Serialize(_value, ref writer);

            _session.FullReset();
            var reader = Reader.Create(writer.Output.GetReadOnlySequence(), _session);
            var result = _hagarSerializer.Deserialize(ref reader);
            HagarBuffer.Reset();
            return result;
        }

        //[Benchmark]
        public object OrleansClassRoundTrip()
        {
            var writer = new BinaryTokenStreamWriter();
            _orleansSerializer.Serialize(_value, writer);
            return _orleansSerializer.Deserialize(new BinaryTokenStreamReader(writer.ToBytes()));
        }

        [Fact]
        //[Benchmark]
        public object HagarSerialize()
        {
            var writer = HagarBuffer.CreateWriter(_session);
            _session.FullReset();
            _hagarSerializer.Serialize(_value, ref writer);
            HagarBuffer.Reset();
            return _session;
        }

        //[Benchmark]
        public object OrleansSerialize()
        {
            var writer = new BinaryTokenStreamWriter();
            _orleansSerializer.Serialize(_value, writer);
            return writer;
        }

        [Fact]
        //[Benchmark]
        public object HagarDeserialize()
        {
            _session.FullReset();
            var reader = Reader.Create(_hagarBytes, _session);
            return _hagarSerializer.Deserialize(ref reader);
        }

        //[Benchmark]
        public object OrleansDeserialize() => _orleansSerializer.Deserialize(new BinaryTokenStreamReader(_orleansBytes));

        [Fact]
        //[Benchmark]
        public int HagarReadEachByte()
        {
            var sum = 0;
            var reader = Reader.Create(_hagarBytes, _session);
            for (var i = 0; i < _readBytesLength; i++)
            {
                sum ^= reader.ReadByte();
            }

            return sum;
        }

        //[Benchmark]
        public int OrleansReadEachByte()
        {
            var sum = 0;
            var reader = new BinaryTokenStreamReader(_orleansBytes);
            for (var i = 0; i < _readBytesLength; i++)
            {
                sum ^= reader.ReadByte();
            }

            return sum;
        }
    }
}