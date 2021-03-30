using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Benchmarks.Utilities;
using FakeFx.Runtime;
using Hagar;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using Xunit;
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    public class MessageBenchmark
    {
        private static readonly Serializer<Message.HeadersContainer> HagarSerializer;
        private static readonly byte[] HagarInput;
        private static readonly SerializerSession Session;
        private static readonly Message.HeadersContainer Value;

        static MessageBenchmark()
        {
            var body = new Response("yess!");
            Value = (new Message
            {
                TargetActivation = ActivationId.NewId(),
                TargetSilo = SiloAddress.New(IPEndPoint.Parse("210.50.4.44:40902"), 5423123),
                TargetGrain = GrainId.Create("sys.mygrain", "borken_thee_doggo"),
                BodyObject = body,
                InterfaceType = GrainInterfaceType.Create("imygrain"),
                SendingActivation = ActivationId.NewId(),
                SendingSilo = SiloAddress.New(IPEndPoint.Parse("10.50.4.44:40902"), 5423123),
                SendingGrain = GrainId.Create("sys.mygrain", "fluffy_g"),
                TraceContext = new TraceContext { ActivityId = Guid.NewGuid() },
                Id = CorrelationId.GetNext()
            }).Headers;

            // Hagar
            var services = new ServiceCollection()
                .AddHagar()
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<Message.HeadersContainer>>();
            var bytes = new byte[4000];
            Session = services.GetRequiredService<SerializerSessionPool>().GetSession();
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            HagarSerializer.Serialize(Value, ref writer);
            HagarInput = bytes;
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
            return HagarSerializer.Serialize(Value, HagarInput, Session);
        }
    }
}