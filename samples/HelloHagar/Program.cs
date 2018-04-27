using System;
using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using MyPocos;

namespace HelloHagar
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(SomeClassWithSerialzers).Assembly)
                .BuildServiceProvider();
            var codecs = serviceProvider.GetRequiredService<ITypedCodecProvider>();

            var codec = codecs.GetCodec<SomeClassWithSerialzers>();

            var sessionPool = serviceProvider.GetRequiredService<SessionPool>();
            var writer = new Writer();
            using (var writerSession = sessionPool.GetSession())
            {
                codec.WriteField(writer,
                    writerSession,
                    0,
                    null,
                    new SomeClassWithSerialzers {IntField = 2, IntProperty = 30});
            }

            var reader = new Reader(writer.ToBytes());
            using (var readerSession = sessionPool.GetSession())
            {
                var initialHeader = reader.ReadFieldHeader(readerSession);
                var result = codec.ReadValue(reader, readerSession, initialHeader);
                Console.WriteLine(result);
            }

            Console.ReadKey();
        }
    }
}
