using System;
using Hagar;
using Hagar.Buffers;
using Hagar.Codec;
using Hagar.Serializer;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using MyPocos;

namespace HelloHagar
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var serviceProvider = new ServiceCollection()
                .AddCryoBuf()
                .AddCryoBufSerializers(typeof(SomeClassWithSerialzers).Assembly)
                .BuildServiceProvider();
            var codecs = serviceProvider.GetRequiredService<ITypedCodecProvider>();

            var codec = codecs.GetCodec<SomeClassWithSerialzers>();

            var writeSession = serviceProvider.GetRequiredService<SerializerSession>();
            var writer = new Writer();
            codec.WriteField(writer,
                writeSession,
                0,
                null,
                new SomeClassWithSerialzers {IntField = 2, IntProperty = 30});

            var reader = new Reader(writer.ToBytes());
            var readerSession = serviceProvider.GetRequiredService<SerializerSession>();
            var initialHeader = reader.ReadFieldHeader(readerSession);
            var result = codec.ReadValue(reader, readerSession, initialHeader);
            Console.WriteLine(result);
            Console.ReadKey();
        }
    }
}
