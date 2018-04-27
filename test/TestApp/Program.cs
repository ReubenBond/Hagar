using System;
using System.Runtime.Serialization;
using System.Text;
using Hagar.Serializers;
using Hagar.Session;
using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.ISerializable;
using Hagar.Json;
using Hagar.Configuration;
using Hagar.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using MyPocos;
using Newtonsoft.Json;
using NodaTime;

namespace TestApp
{
    public class Program
    {
        public static void TestOne()
        {
            Console.WriteLine("Hello World!");
            var serviceProvider = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(SomeClassWithSerialzers).Assembly)
                .AddHagar(options =>
                {
                    options.PartialSerializers.Add(typeof(SubTypeSerializer));
                    options.PartialSerializers.Add(typeof(BaseTypeSerializer));
                })
                .BuildServiceProvider();

            var c = serviceProvider.GetRequiredService<IPartialSerializer<SubType>>();
            c.Serialize(new Writer(), serviceProvider.GetRequiredService<SessionPool>().GetSession(), new SubType());
            var codecs = serviceProvider.GetRequiredService<ITypedCodecProvider>();

            var codec = codecs.GetCodec<SomeClassWithSerialzers>();
            var sessionPool = serviceProvider.GetRequiredService<SessionPool>();

            var writeSession = sessionPool.GetSession();
            var writer = new Writer();
            codec.WriteField(writer,
                             writeSession,
                             0,
                             typeof(SomeClassWithSerialzers),
                             new SomeClassWithSerialzers { IntField = 2, IntProperty = 30 });

            using (var readerSession = sessionPool.GetSession())
            {
                var reader = new Reader(writer.ToBytes());
                Console.WriteLine(string.Join(" ", TokenStreamParser.Parse(reader, readerSession)));
            }

            using (var readerSession = sessionPool.GetSession())
            {
                var reader = new Reader(writer.ToBytes());
                var initialHeader = reader.ReadFieldHeader(readerSession);
                var result = codec.ReadValue(reader, readerSession, initialHeader);
                Console.WriteLine(result);
            }
        }

        public static void Main(string[] args)
        {
            TestOne();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHagar(configuration =>
            {
                configuration.PartialSerializers.Add(typeof(SubTypeSerializer));
                configuration.PartialSerializers.Add(typeof(BaseTypeSerializer));
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
                new AbstractTypeSerializer<object>(codecProvider),
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
                new AbstractTypeSerializer<object>(codecProvider),
                mySerializable
            );

            mySerializable.Self = mySerializable;
            Test(
                GetSession,
                new AbstractTypeSerializer<object>(codecProvider),
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
                new AbstractTypeSerializer<object>(codecProvider),
                exception
            );

            Test(GetSession, new AbstractTypeSerializer<object>(codecProvider), new LocalDate());

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
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(T), expected);

            Console.WriteLine($"Size: {writer.CurrentOffset} bytes.");
            Console.WriteLine($"Wrote References:\n{GetWriteReferenceTable(session)}");

            //Console.WriteLine("TokenStream: " + string.Join(" ", TokenStreamParser.Parse(new Reader(writer.ToBytes()), getSession())));

            var reader = new Reader(writer.ToBytes());
            var deserializationContext = getSession();
            var initialHeader = reader.ReadFieldHeader(session);
            //Console.WriteLine(initialHeader);
            var actual = serializer.ReadValue(reader, deserializationContext, initialHeader);

            Console.WriteLine($"Expect: {expected}\nActual: {actual}");
            var references = GetReadReferenceTable(deserializationContext);
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
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(SubType), expected);
            
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = getSession();
            var initialHeader = reader.ReadFieldHeader(session);
            var skipCodec = new SkipFieldCodec();
            skipCodec.ReadValue(reader, deserializationContext, initialHeader);
            
            Console.WriteLine($"Skipped {reader.CurrentPosition}/{reader.Length} bytes.");
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