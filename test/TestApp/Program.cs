using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.Invocation;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MyPocos;
using Newtonsoft.Json;
using NodaTime;
using System;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    [GenerateMethodSerializers(typeof(MyProxyBaseClass))]
    public interface IGrain { }

    [GenerateMethodSerializers(typeof(MyProxyBaseClass), isExtension: true)]
    public interface IGrainExtension { }

    public class Program
    {
        public static async Task TestRpc()
        {
            Console.WriteLine("Hello World!");
            var serviceProvider = new ServiceCollection()
                .AddHagar()
                .BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IConfiguration<SerializerConfiguration>>();
            var allProxies = config.Value.InterfaceProxies;

            var proxy = GetProxy<IMyInvokable>();
            _ = await proxy.Multiply(4, 5, "hello");
            var proxyBase = proxy as MyProxyBaseClass;
            using var invocation = proxyBase.Invocations.First();
            invocation.SetTarget(new TargetHolder(new MyImplementation()));
            _ = await invocation.Invoke();

            var generic = GetProxy<IMyInvokable<int>>();
            //((MyProxyBaseClass)generic).Invocations.Find()
            await generic.DoStuff<string>();

            TInterface GetProxy<TInterface>()
            {
                if (typeof(TInterface).IsGenericType)
                {
                    var unbound = typeof(TInterface).GetGenericTypeDefinition();
                    var parameters = typeof(TInterface).GetGenericArguments();
                    foreach (var proxyType in allProxies)
                    {
                        if (!proxyType.IsGenericType)
                        {
                            continue;
                        }

                        var matching = proxyType.FindInterfaces(
                            (type, criteria) => type.IsGenericType && type.GetGenericTypeDefinition() == (Type)criteria,
                            unbound).FirstOrDefault();
                        if (matching != null)
                        {
                            return (TInterface)Activator.CreateInstance(proxyType.GetGenericTypeDefinition().MakeGenericType(parameters));
                        }
                    }
                }

                return (TInterface)Activator.CreateInstance(
                    allProxies.First(p => typeof(TInterface).IsAssignableFrom(p)));
            }
        }

        internal struct TargetHolder : ITargetHolder
        {
            private readonly object _target;

            public TargetHolder(object target)
            {
                _target = target;
            }

            public TTarget GetTarget<TTarget>() => (TTarget)_target;

            public TExtension GetComponent<TExtension>() => throw new NotImplementedException();
        }

        internal class MyImplementation : IMyInvokable
        {
            public ValueTask<int> Multiply(int a, int b, object c) => new(a * b);
        }
        internal class MyImplementation<T> : IMyInvokable<T>
        {
            public Task DoStuff<TU>() => Task.CompletedTask;
        }

        public static void TestOne()
        {
            Console.WriteLine("Hello World!");
            var serviceProvider = new ServiceCollection()
                .AddHagar()
                .BuildServiceProvider();

            var codecs = serviceProvider.GetRequiredService<ICodecProvider>();

            var codec = codecs.GetCodec<SomeClassWithSerialzers>();
            var sessionPool = serviceProvider.GetRequiredService<SerializerSessionPool>();

            var writeSession = sessionPool.GetSession();
            var pipe = new Pipe();
            var writer = Writer.Create(pipe.Writer, writeSession);
            codec.WriteField(ref writer,
                             0,
                             typeof(SomeClassWithSerialzers),
                             new SomeClassWithSerialzers { IntField = 2, IntProperty = 30 });
            writer.Commit();
            _ = pipe.Writer.FlushAsync().AsTask().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            _ = pipe.Reader.TryRead(out var readResult);

            {
                using var readerSession = sessionPool.GetSession();
                var reader = Reader.Create(readResult.Buffer, readerSession);
                var result = BitStreamFormatter.Format(ref reader);
                Console.WriteLine(result);
            }

            {
                using var readerSession = sessionPool.GetSession();
                var reader = Reader.Create(readResult.Buffer, readerSession);
                var initialHeader = reader.ReadFieldHeader();
                var result = codec.ReadValue(ref reader, initialHeader);
                Console.WriteLine(result);
            }
        }

        public static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddHagar()
                .BuildServiceProvider();

            SerializerSession GetSession() => serviceProvider.GetRequiredService<SerializerSessionPool>().GetSession();
            //TestRpc().GetAwaiter().GetResult();
            //return;
            TestOne();

            /*
            Test(
                GetSession,
                new AbstractTypeSerializer<object>(),
                new WhackyJsonType
                {
                    Number = 7,
                    String = "bananas!"
                });
            */
            var mySerializable = new MySerializableClass
            {
                String = "yolooo",
                Integer = 38,
                Self = null,
            };
            Test(
                GetSession,
                new AbstractTypeSerializer<object>(),
                mySerializable
            );

            mySerializable.Self = mySerializable;
            Test(
                GetSession,
                new AbstractTypeSerializer<object>(),
                mySerializable
            );

            /*
            Exception exception = null;
            try
            {
                throw new ReferenceNotFoundException(typeof(int), 2401);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Test(
                GetSession,
                new AbstractTypeSerializer<object>(),
                exception
            );
            */

            Test(GetSession, new AbstractTypeSerializer<object>(), new LocalDate());

            Console.WriteLine("Press any key to exit.");
            _ = Console.ReadKey();
        }

        private static void Test<T>(Func<SerializerSession> getSession, IFieldCodec<T> serializer, T expected)
        {
            using var writerSession = getSession();
            var pipe = new Pipe();
            var writer = Writer.Create(pipe.Writer, writerSession);

            serializer.WriteField(ref writer, 0, typeof(T), expected);
            writer.Commit();

            Console.WriteLine($"Size: {writer.Position} bytes.");
            Console.WriteLine($"Wrote References:\n{GetWriteReferenceTable(writerSession)}");

            _ = pipe.Writer.FlushAsync().AsTask().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            _ = pipe.Reader.TryRead(out var readResult);
            {
                using var readerSesssion = getSession();
                var reader = Reader.Create(readResult.Buffer, readerSesssion);
                var result = BitStreamFormatter.Format(ref reader);
                Console.WriteLine(result);
            }
            {
                using var readerSesssion = getSession();
                var reader = Reader.Create(readResult.Buffer, readerSesssion);
                var initialHeader = reader.ReadFieldHeader();

                Console.WriteLine("Header:");
                Console.WriteLine(initialHeader.ToString());

                var actual = serializer.ReadValue(ref reader, initialHeader);
                pipe.Reader.AdvanceTo(readResult.Buffer.End);
                pipe.Reader.Complete();

                Console.WriteLine($"Expect: {expected}\nActual: {actual}");

                var references = GetReadReferenceTable(reader.Session);
                Console.WriteLine($"Read references:\n{references}");
            }
        }

        private static StringBuilder GetReadReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyReferenceTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                _ = references.AppendLine($"\t[{entry.Key}] {entry.Value}");
            }
            return references;
        }

        private static StringBuilder GetWriteReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyIdTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                _ = references.AppendLine($"\t[{entry.Value}] {entry.Key}");
            }
            return references;
        }

        [Serializable]
        public class MySerializableClass : ISerializable
        {
            public string String { get; set; }
            public int Integer { get; set; }
            public MySerializableClass Self { get; set; }

            public MySerializableClass()
            {
            }

            protected MySerializableClass(SerializationInfo info, StreamingContext context)
            {
                String = info.GetString(nameof(String));
                Integer = info.GetInt32(nameof(Integer));
                Self = (MySerializableClass)info.GetValue(nameof(Self), typeof(MySerializableClass));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(String), String);
                info.AddValue(nameof(Integer), Integer);
                info.AddValue(nameof(Self), Self);
            }

            public override string ToString()
            {
                string refString = Self == this ? "[this]" : $"[{Self?.ToString() ?? "null"}]";
                return $"{base.ToString()}, {nameof(String)}: {String}, {nameof(Integer)}: {Integer}, Self: {refString}";
            }
        }
    }

    public class WhackyJsonType
    {
        public int Number { get; set; }
        public string String { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }


}