using System;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Hagar.Serializers;
using Hagar.Session;
using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.Invocation;
using Hagar.ISerializable;
using Hagar.Json;
using Microsoft.Extensions.DependencyInjection;
using MyPocos;
using Newtonsoft.Json;
using NodaTime;

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
                .AddSerializers(typeof(SomeClassWithSerialzers).Assembly)
                .BuildServiceProvider();

            var config = serviceProvider.GetRequiredService<IConfiguration<SerializerConfiguration>>();
            var allProxies = config.Value.InterfaceProxies;

            var proxy = GetProxy<IMyInvokable>();
            await proxy.Multiply(4, 5, "hello");
            var proxyBase = proxy as MyProxyBaseClass;
            var invocation = proxyBase.Invocations.First();
            invocation.SetTarget(new TargetHolder(new MyImplementation()));
            await invocation.Invoke();
            invocation.Reset();

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
                        if (!proxyType.IsGenericType) continue;
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
            private readonly object target;

            public TargetHolder(object target)
            {
                this.target = target;
            }

            public TTarget GetTarget<TTarget>() => (TTarget)this.target;

            public TExtension GetExtension<TExtension>() => throw new NotImplementedException();
        }

        internal class MyImplementation : IMyInvokable
        {
            public ValueTask<int> Multiply(int a, int b, object c) => new ValueTask<int>(a * b);
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
                .AddSerializers(typeof(SomeClassWithSerialzers).Assembly)
                .AddHagar(options =>
                {
                    options.Serializers.Add(typeof(SubTypeSerializer));
                    options.Serializers.Add(typeof(BaseTypeSerializer));
                })
                .BuildServiceProvider();

            using (var serializerSession = serviceProvider.GetRequiredService<SessionPool>().GetSession())
            {
                var c = serviceProvider.GetRequiredService<IPartialSerializer<SubType>>();
                var p = new Pipe();
                var w = new Writer<PipeWriter>(p.Writer, serializerSession);
                c.Serialize(ref w, new SubType());
                p.Writer.Complete();
            }

            var codecs = serviceProvider.GetRequiredService<ITypedCodecProvider>();

            var codec = codecs.GetCodec<SomeClassWithSerialzers>();
            var sessionPool = serviceProvider.GetRequiredService<SessionPool>();

            var writeSession = sessionPool.GetSession();
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, writeSession);
            codec.WriteField(ref writer,
                             0,
                             typeof(SomeClassWithSerialzers),
                             new SomeClassWithSerialzers { IntField = 2, IntProperty = 30 });
            writer.Commit();
            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            pipe.Reader.TryRead(out var readResult);

            using (var readerSession = sessionPool.GetSession())
            {
                var reader = new Reader(readResult.Buffer, readerSession);
                var initialHeader = reader.ReadFieldHeader();
                var result = codec.ReadValue(ref reader, initialHeader);
                Console.WriteLine(result);
            }
        }

        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHagar(configuration =>
            {
                configuration.Serializers.Add(typeof(SubTypeSerializer));
                configuration.Serializers.Add(typeof(BaseTypeSerializer));
            });
            serviceCollection.AddSingleton<IGeneralizedCodec, DotNetSerializableCodec>();
            serviceCollection.AddSingleton<IGeneralizedCodec, JsonCodec>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var codecProvider = serviceProvider.GetRequiredService<CodecProvider>();
            var serializer = codecProvider.GetCodec<SubType>();
            SerializerSession GetSession() => serviceProvider.GetRequiredService<SessionPool>().GetSession();
            var testString = "hello, hagar";


            Test(
                GetSession,
                serializer,
                new SubType
                {
                    BaseTypeString = "HOHOHO",
                    AddedLaterString = testString,
                    String = null,
                    Int = 1,
                    Ref = testString
                });
            TestRpc().GetAwaiter().GetResult();
            //return;
            TestOne();
            Test(
                GetSession,
                serializer,
                new SubType
                {
                    BaseTypeString = "base",
                    String = "sub",
                    Int = 2,
                });
            Test(
                GetSession,
                serializer,
                new SubType
                {
                    BaseTypeString = "base",
                    String = "sub",
                    Int = int.MinValue,
                });

            // Tests for duplicates
            Test(
                GetSession,
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = testString,
                    Int = 10
                });
            Test(
                GetSession,
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });
            Test(
                GetSession,
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });
            TestSkip(
                GetSession,
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });
            
            var self = new SubType
            {
                BaseTypeString = "HOHOHO",
                AddedLaterString = testString,
                String = null,
                Int = 1
            };
            self.Ref = self;
            Test(GetSession, serializer, self);

            self.Ref = Guid.NewGuid();
            Test(GetSession, serializer, self);

            Test(
                GetSession,
                new AbstractTypeSerializer<object>(),
                new WhackyJsonType
                {
                    Number = 7,
                    String = "bananas!"
                });
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

            Test(GetSession, new AbstractTypeSerializer<object>(), new LocalDate());

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
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
                this.String = info.GetString(nameof(this.String));
                this.Integer = info.GetInt32(nameof(this.Integer));
                this.Self = (MySerializableClass) info.GetValue(nameof(this.Self), typeof(MySerializableClass));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(this.String), this.String);
                info.AddValue(nameof(this.Integer), this.Integer);
                info.AddValue(nameof(this.Self), this.Self);
            }

            public override string ToString()
            {
                string refString = this.Self == this ? "[this]" : $"[{this.Self?.ToString() ?? "null"}]";
                return $"{base.ToString()}, {nameof(this.String)}: {this.String}, {nameof(this.Integer)}: {this.Integer}, Self: {refString}";
            }

        }

        static void Test<T>(Func<SerializerSession> getSession, IFieldCodec<T> serializer, T expected)
        {
            var session = getSession();
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, session);

            serializer.WriteField(ref writer, 0, typeof(T), expected);
            writer.Commit();

            Console.WriteLine($"Size: {writer.Position} bytes.");
            Console.WriteLine($"Wrote References:\n{GetWriteReferenceTable(session)}");

            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            pipe.Reader.TryRead(out var readResult);
            var reader = new Reader(readResult.Buffer, getSession());
            var initialHeader = reader.ReadFieldHeader();
            //Console.WriteLine(initialHeader);
            var actual = serializer.ReadValue(ref reader, initialHeader);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();

            Console.WriteLine($"Expect: {expected}\nActual: {actual}");
            var references = GetReadReferenceTable(reader.Session);
            Console.WriteLine($"Read references:\n{references}");
        }

        private static StringBuilder GetReadReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyReferenceTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                references.AppendLine($"\t[{entry.Key}] {entry.Value}");
            }
            return references;
        }
        private static StringBuilder GetWriteReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyIdTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                references.AppendLine($"\t[{entry.Value}] {entry.Key}");
            }
            return references;
        }

        static void TestSkip(Func<SerializerSession> getSession, IFieldCodec<SubType> serializer, SubType expected)
        {
            var session = getSession();
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer, session);

            serializer.WriteField(ref writer, 0, typeof(SubType), expected);
            writer.Commit();

            pipe.Writer.FlushAsync().GetAwaiter().GetResult();
            pipe.Writer.Complete();
            pipe.Reader.TryRead(out var readResult);
            var reader = new Reader(readResult.Buffer, getSession());
            var initialHeader = reader.ReadFieldHeader();
            var skipCodec = new SkipFieldCodec();
            skipCodec.ReadValue(ref reader, initialHeader);
            pipe.Reader.AdvanceTo(readResult.Buffer.End);
            pipe.Reader.Complete();
            Console.WriteLine($"Skipped {reader.Position} bytes.");
        }
    }

    public class BaseType
    {
        public string BaseTypeString { get; set; }
        public string AddedLaterString { get; set; }

        public override string ToString()
        {
            return $"{nameof(this.BaseTypeString)}: {this.BaseTypeString}";
        }
    }

    /// <summary>
    /// NOTE: The serializer for this type is HAND-ROLLED. See <see cref="SubTypeSerializer" />
    /// </summary>
    public class SubType : BaseType
    {
        // 0
        public string String { get; set; }

        // 1
        public int Int { get; set; }
        
        // 3
        public object Ref { get; set; }

        public override string ToString()
        {
            string refString = this.Ref == this ? "[this]" : $"[{this.Ref?.ToString() ?? "null"}]";
            return $"{base.ToString()}, {nameof(this.String)}: {this.String}, {nameof(this.Int)}: {this.Int}, Ref: {refString}";
        }
    }

    public class WhackyJsonType
    {
        public int Number { get; set; }
        public string String { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}