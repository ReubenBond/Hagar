using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Buffers.Adaptors;
using Hagar.Codecs;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using Xunit;
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    public class MegaGraphBenchmark
    {
        private static readonly Serializer<Dictionary<string, int>> HagarSerializer;
        private static readonly byte[] HagarInput;

        public static StringDictionaryCodec<int> OptimizedCodec { get; }

        private static readonly SerializerSession Session;
        private static readonly Dictionary<string, int> Value;

        static MegaGraphBenchmark()
        {
            const int Size = 250_000;
            Value = new Dictionary<string, int>(Size);
            for (var i = 0; i < Size; i++)
            {
                Value[i.ToString(CultureInfo.InvariantCulture)] = i;
            }
            
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .AddSingleton<StringKeyValuePairCodec<int>>()
                .AddSingleton<StringDictionaryCodec<int>>()
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<Dictionary<string, int>>>();
            Session = services.GetRequiredService<SerializerSessionPool>().GetSession();
            var pipe = new Pipe(new PipeOptions(readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline));
            var writer = pipe.Writer.CreateWriter(Session);
            HagarSerializer.Serialize(Value, ref writer);
            pipe.Writer.FlushAsync();
            pipe.Reader.TryRead(out var result);
            HagarInput = result.Buffer.ToArray();
            OptimizedCodec = services.GetRequiredService<StringDictionaryCodec<int>>();
        }

        [Fact]
        [Benchmark]
        public object Deserialize()
        {
            Session.FullReset();
            var instance = HagarSerializer.Deserialize(HagarInput, Session);
            return instance;
        }

        [Fact]
        [Benchmark]
        public int Serialize()
        {
            Session.FullReset();
            var writer = Writer.Create(new PooledArrayBufferWriter(4096), Session);
            HagarSerializer.Serialize(Value, ref writer);
            writer.Output.Dispose();
            return writer.Position;
        }

        [Fact]
        [Benchmark]
        public object Deserialize2()
        {
            Session.FullReset();
            var reader = Reader.Create(HagarInput, Session);
            var field = reader.ReadFieldHeader();
            return OptimizedCodec.ReadValue(ref reader, field);
        }

        [Fact]
        [Benchmark]
        public int Serialize2()
        {
            Session.FullReset();
            var writer = Writer.Create(new PooledArrayBufferWriter(4096), Session);
            OptimizedCodec.WriteField(ref writer, 0, typeof(Dictionary<string, int>), Value);
            writer.Commit();
            writer.Output.Dispose();
            return writer.Position;
        }
    }
}