using System;
using System.IO.Pipelines;
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
            var serializer = serviceProvider.GetRequiredService<Serializer<SomeClassWithSerialzers>>();
            
            var sessionPool = serviceProvider.GetRequiredService<SessionPool>();
            var pipe = new Pipe();

            using (var session = sessionPool.GetSession())
            {
                var writer = pipe.Writer.CreateWriter(session);
                serializer.Serialize(ref writer, new SomeClassWithSerialzers {IntField = 2, IntProperty = 30});
                pipe.Writer.Complete();
            }

            using (var session = sessionPool.GetSession())
            {
                pipe.Reader.TryRead(out var readResult);
                var reader = new Reader(readResult.Buffer, session);
                var result = serializer.Deserialize(ref reader);
                Console.WriteLine(result);
            }

            Console.ReadKey();
        }
    }
}
