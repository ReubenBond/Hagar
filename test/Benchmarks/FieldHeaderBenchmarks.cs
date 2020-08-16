using BenchmarkDotNet.Attributes;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.WireProtocol;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks
{
    [Config(typeof(BenchmarkConfig))]
    public class FieldHeaderBenchmarks
    {
        private static readonly SerializerSession Session;
        private static readonly byte[] HagarBuffer = new byte[1000];

        static FieldHeaderBenchmarks()
        {
            var services = new ServiceCollection();
            _ = services
                .AddHagar(hagar =>
                    hagar.AddISerializableSupport()
                        .AddAssembly(typeof(Program).Assembly));
            var serviceProvider = services.BuildServiceProvider();
            var sessionPool = serviceProvider.GetRequiredService<SessionPool>();
            Session = sessionPool.GetSession();
        }

        [Benchmark(Baseline = true)]
        public void WritePlainExpectedEmbeddedId()
        {
            var writer = new SingleSegmentBuffer(HagarBuffer).CreateWriter(Session);

            // Use an expected type and a field id with a value small enough to be embedded.
            writer.WriteFieldHeader(4, typeof(uint), typeof(uint), WireType.VarInt);
        }

        [Benchmark]
        public void WritePlainExpectedExtendedId()
        {
            var writer = new SingleSegmentBuffer(HagarBuffer).CreateWriter(Session);

            // Use a field id delta which is too large to be embedded.
            writer.WriteFieldHeader(Tag.MaxEmbeddedFieldIdDelta + 20, typeof(uint), typeof(uint), WireType.VarInt);
        }

        [Benchmark]
        public void WriteFastEmbedded()
        {
            var writer = new SingleSegmentBuffer(HagarBuffer).CreateWriter(Session);

            // Use an expected type and a field id with a value small enough to be embedded.
            writer.WriteFieldHeaderExpectedEmbedded(4, WireType.VarInt);
        }

        [Benchmark]
        public void WriteFastExtended()
        {
            var writer = new SingleSegmentBuffer(HagarBuffer).CreateWriter(Session);

            // Use a field id delta which is too large to be embedded.
            writer.WriteFieldHeaderExpectedExtended(Tag.MaxEmbeddedFieldIdDelta + 20, WireType.VarInt);
        }

        [Benchmark]
        public void CreateWriter() => _ = new SingleSegmentBuffer(HagarBuffer).CreateWriter(Session);

        [Benchmark]
        public void WriteByte()
        {
            var writer = new SingleSegmentBuffer(HagarBuffer).CreateWriter(Session);
            writer.Write((byte)4);
        }
    }
}