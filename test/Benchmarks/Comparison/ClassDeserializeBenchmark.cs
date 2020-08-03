using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Benchmarks.Models;
using Benchmarks.Utilities;
using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;
using Hyperion;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Xunit;
using ZeroFormatter;
using SerializerSession = Hagar.Session.SerializerSession;

namespace Benchmarks.Comparison
{
    [Trait("Category", "Benchmark")]
    [Config(typeof(BenchmarkConfig))]
    [DisassemblyDiagnoser(maxDepth: 2, printSource: true)]
    [EventPipeProfiler(BenchmarkDotNet.Diagnosers.EventPipeProfile.CpuSampling)]
    //[EtwProfiler]
    public class ClassDeserializeBenchmark
    {
        private static readonly MemoryStream ProtoInput;

        private static readonly byte[] MsgPackInput = MessagePack.MessagePackSerializer.Serialize(IntClass.Create());

        private static readonly string NewtonsoftJsonInput = JsonConvert.SerializeObject(IntClass.Create());

        private static readonly byte[] SpanJsonInput = SpanJson.JsonSerializer.Generic.Utf8.Serialize(IntClass.Create());

        private static readonly Hyperion.Serializer HyperionSerializer = new Hyperion.Serializer(new SerializerOptions(knownTypes: new[] { typeof(IntClass) }));
        private static readonly MemoryStream HyperionInput;

        private static readonly Serializer<IntClass> HagarSerializer;
        private static readonly byte[] HagarInput;
        private static readonly SerializerSession Session;

        private static readonly SerializationManager OrleansSerializer;
        private static readonly List<ArraySegment<byte>> OrleansInput;
        private static readonly BinaryTokenStreamReader OrleansBuffer;
        private static readonly HagarGen_Serializer_IntClass_1843466 generated = new HagarGen_Serializer_IntClass_1843466();

        static ClassDeserializeBenchmark()
        {
            ProtoInput = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ProtoInput, IntClass.Create());

            HyperionInput = new MemoryStream();
            HyperionSerializer.Serialize(IntClass.Create(), HyperionInput);

            // Hagar
            var services = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(Program).Assembly))
                .BuildServiceProvider();
            HagarSerializer = services.GetRequiredService<Serializer<IntClass>>();
            var bytes = new byte[1000];
            Session = services.GetRequiredService<SessionPool>().GetSession();
            var writer = new SingleSegmentBuffer(bytes).CreateWriter(Session);
            //HagarSerializer.Serialize(ref writer, IntClass.Create());
            writer.WriteStartObject(0, typeof(IntClass), typeof(IntClass));
            generated.Serialize(ref writer, IntClass.Create());
            writer.WriteEndObject();
            HagarInput = bytes;

            // Orleans
            OrleansSerializer = new ClientBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering()
                .ConfigureServices(s => s.ToList().ForEach(r =>
                {
                    if (r.ServiceType == typeof(IConfigurationValidator)) s.Remove(r);
                }))
                .Configure<ClusterOptions>(o => o.ClusterId = o.ServiceId = "test")
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SimpleClass).Assembly).WithCodeGeneration())
                .Configure<SerializationProviderOptions>(options => options.FallbackSerializationProvider = typeof(SupportsNothingSerializer).GetTypeInfo())
                .Build().ServiceProvider.GetRequiredService<SerializationManager>();

            var writer2 = new BinaryTokenStreamWriter();
            OrleansSerializer.Serialize(IntClass.Create(), writer2);
            OrleansInput = writer2.ToBytes();
            OrleansBuffer = new BinaryTokenStreamReader(OrleansInput);
        }

        private static int SumResult(IntClass result)
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

        private static int SumResult(VirtualIntsClass result)
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

        [Fact]
        [Benchmark(Baseline = true)]
        public int Hagar()
        {
            Session.FullReset();
            var reader = new Reader(new ReadOnlySequence<byte>(HagarInput), Session);
            var instance = new IntClass();
            reader.ReadFieldHeader();
            generated.DeserializeNew(ref reader, instance);
            return SumResult(instance);
        }

        [Benchmark]
        public int Orleans()
        {
            OrleansBuffer.Reset(OrleansInput);
            return SumResult(OrleansSerializer.Deserialize<IntClass>(OrleansBuffer));
        }

        [Benchmark]
        public int MessagePackCSharp()
        {
            return SumResult(MessagePack.MessagePackSerializer.Deserialize<IntClass>(MsgPackInput));
        }

        [Benchmark]
        public int ProtobufNet()
        {
            ProtoInput.Position = 0;
            return SumResult(ProtoBuf.Serializer.Deserialize<IntClass>(ProtoInput));
        }

        [Benchmark]
        public int Hyperion()
        {
            HyperionInput.Position = 0;
            return SumResult(HyperionSerializer.Deserialize<IntClass>(HyperionInput));
        }

        [Benchmark]
        public int NewtonsoftJson()
        {
            return SumResult(JsonConvert.DeserializeObject<IntClass>(NewtonsoftJsonInput));
        }

        [Benchmark(Description = "SpanJson")]
        public int SpanJsonUtf8()
        {
            return SumResult(SpanJson.JsonSerializer.Generic.Utf8.Deserialize<IntClass>(SpanJsonInput));
        }
    }

    internal sealed class HagarGen_Serializer_IntClass_1843466 : global::Hagar.Serializers.IPartialSerializer<global::Benchmarks.Models.IntClass>
    {
        private static readonly global::System.Type int32Type = typeof(int);
        public HagarGen_Serializer_IntClass_1843466()
        {
        }

        public void Serialize<TBufferWriter>(ref global::Hagar.Buffers.Writer<TBufferWriter> writer, global::Benchmarks.Models.IntClass instance)
            where TBufferWriter : global::System.Buffers.IBufferWriter<byte>
        {
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 0U, int32Type, instance.MyProperty1);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty2);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty3);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty4);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty5);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty6);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty7);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty8);
            global::Hagar.Codecs.Int32Codec.WriteField(ref writer, 1U, int32Type, instance.MyProperty9);
        }

        public void Deserialize(ref global::Hagar.Buffers.Reader reader, global::Benchmarks.Models.IntClass instance)
        {
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                    break;
                fieldId += header.FieldIdDelta;
                switch ((fieldId))
                {
                    case 0U:
                        instance.MyProperty1 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 1U:
                        instance.MyProperty2 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 2U:
                        instance.MyProperty3 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 3U:
                        instance.MyProperty4 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 4U:
                        instance.MyProperty5 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 5U:
                        instance.MyProperty6 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 6U:
                        instance.MyProperty7 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 7U:
                        instance.MyProperty8 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    case 8U:
                        instance.MyProperty9 = (int)global::Hagar.Codecs.Int32Codec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }
        }
        public void DeserializeNew(ref global::Hagar.Buffers.Reader reader, global::Benchmarks.Models.IntClass instance)
        {
            int id = 0;
            Field header = default;
            while (true)
            {
                id = ReadHeader(ref reader, ref header, id);

                if (id == 0)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty1 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 1)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty2 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 2)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty3 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 3)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty4 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 4)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty5 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 5)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty6 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 6)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty7 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 7)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty8 = reader.ReadInt32(header.WireType);
                    id = ReadHeader(ref reader, ref header, id);
                }

                if (id == 8)
                {
                    ReferenceCodec.MarkValueField(reader.Session);
                    instance.MyProperty9 = reader.ReadInt32(header.WireType);
                    id = ReadHeaderExpectingEndBaseOrEndObject(ref reader, ref header, id);
                }

                if (id == -1)
                {
                    break;
                }

                reader.ConsumeUnknownField(header);
            }
        }

         private static int ReadHeader(ref Reader reader, ref Field header, int id)
        {
            reader.ReadFieldHeader(ref header);
            if (header.IsEndBaseOrEndObject) return -1;
            return (int)(id + header.FieldIdDelta);
        }
        
        private static int ReadHeaderExpectingEndBaseOrEndObject(ref Reader reader, ref Field header, int id)
        {
            reader.ReadFieldHeader(ref header);
            if (header.IsEndBaseOrEndObject) return -1;
            return (int)(id + header.FieldIdDelta);
        }
    }
}