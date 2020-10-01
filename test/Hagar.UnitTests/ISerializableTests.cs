using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Hagar.UnitTests
{
    [Trait("Category", "BVT"), Trait("Category", "ISerializable")]
    public class ISerializableTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SessionPool _sessionPool;
        private readonly Serializer<object> _serializer;

        public ISerializableTests()
        {
            var services = new ServiceCollection();
            _ = services.AddHagar(hagar => hagar.AddISerializableSupport());

            _serviceProvider = services.BuildServiceProvider();
            _sessionPool = _serviceProvider.GetService<SessionPool>();
            _serializer = _serviceProvider.GetRequiredService<Serializer<object>>();
        }

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        private object DotNetSerializationLoop(object input)
        {
            byte[] bytes;
            object deserialized;
            var formatter = new BinaryFormatter
            {
                Context = new StreamingContext(StreamingContextStates.All, null)
            };
            using (var str = new MemoryStream())
            {
                formatter.Serialize(str, input);
                str.Flush();
                bytes = str.ToArray();
            }
            using (var inStream = new MemoryStream(bytes))
            {
                deserialized = formatter.Deserialize(inStream);
            }
            return deserialized;
        }
#pragma warning restore SYSLIB0011 // Type or member is obsolete

        private object SerializationLoop(object expected)
        {
            var pipe = new Pipe();

            using (var session = _sessionPool.GetSession())
            {
                var writer = Writer.Create(pipe.Writer, session);
                _serializer.Serialize(ref writer, expected);
                _ = pipe.Writer.FlushAsync().GetAwaiter().GetResult();
                pipe.Writer.Complete();
            }

            using (var session = _sessionPool.GetSession())
            {
                _ = pipe.Reader.TryRead(out var readResult);
                var reader = Reader.Create(readResult.Buffer, session);
                var result = _serializer.Deserialize(ref reader);
                pipe.Reader.AdvanceTo(readResult.Buffer.End);
                pipe.Reader.Complete();
                return result;
            }
        }

        /// <summary>
        /// Tests that <see cref="Hagar.ISerializable.DotNetSerializableCodec"/> can correctly serialize objects.
        /// </summary>
        [Fact]
        public void ISerializableObjectWithCallbacks()
        {
            var input = new SimpleISerializableObject
            {
                Payload = "pyjamas"
            };

            // Verify that our behavior conforms to our expected behavior.
            var result = (SimpleISerializableObject)SerializationLoop(input);
            Assert.Equal(
                new[]
                {
                    "default_ctor",
                    "serializing",
                    "serialized"
                },
                input.History);
            Assert.Equal(3, input.Contexts.Count);
            //Assert.All(input.Contexts, ctx => Assert.True(ctx.Context is ICopyContext || ctx.Context is ISerializationContext));

            Assert.Equal(
                new[]
                {
                    "deserializing",
                    "serialization_ctor",
                    "deserialized",
                    "deserialization"
                },
                result.History);
            Assert.Equal(input.Payload, result.Payload, StringComparer.Ordinal);
            Assert.Equal(3, result.Contexts.Count);
            //Assert.All(result.Contexts, ctx => Assert.True(ctx.Context is IDeserializationContext));

            // Verify that our behavior conforms to the behavior of BinaryFormatter.
            var input2 = new SimpleISerializableObject
            {
                Payload = "pyjamas"
            };

            var result2 = (SimpleISerializableObject)DotNetSerializationLoop(input2);

            Assert.Equal(input2.History, input.History);
            Assert.Equal(result2.History, result.History);
        }

        /// <summary>
        /// Tests that <see cref="Hagar.ISerializable.DotNetSerializableCodec"/> can correctly serialize structs.
        /// </summary>
        [Fact]
        public void ISerializableStructWithCallbacks()
        {
            var input = new SimpleISerializableStruct
            {
                Payload = "pyjamas"
            };

            // Verify that our behavior conforms to our expected behavior.
            var result = (SimpleISerializableStruct)SerializationLoop(input);
            Assert.Equal(
                new[]
                {
                    "serialization_ctor",
                    "deserialized",
                    "deserialization"
                },
                result.History);
            Assert.Equal(input.Payload, result.Payload, StringComparer.Ordinal);
            Assert.Equal(2, result.Contexts.Count);
            //Assert.All(result.Contexts, ctx => Assert.True(ctx.Context is IDeserializationContext));

            // Verify that our behavior conforms to the behavior of BinaryFormatter.
            var input2 = new SimpleISerializableStruct
            {
                Payload = "pyjamas"
            };

            var result2 = (SimpleISerializableStruct)DotNetSerializationLoop(input2);

            Assert.Equal(input2.History, input.History);
            Assert.Equal(result2.History, result.History);
        }

        [Serializable]
        public class SimpleISerializableObject : System.Runtime.Serialization.ISerializable, IDeserializationCallback
        {
            private List<string> _history;
            private List<StreamingContext> _contexts;

            public SimpleISerializableObject()
            {
                History.Add("default_ctor");
            }

            public SimpleISerializableObject(SerializationInfo info, StreamingContext context)
            {
                History.Add("serialization_ctor");
                Contexts.Add(context);
                Payload = info.GetString(nameof(Payload));
            }

            public List<string> History => _history ??= new List<string>();
            public List<StreamingContext> Contexts => _contexts ??= new List<StreamingContext>();

            public string Payload { get; set; }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                Contexts.Add(context);
                info.AddValue(nameof(Payload), Payload);
            }

            [OnSerializing]
            internal void OnSerializingMethod(StreamingContext context)
            {
                History.Add("serializing");
                Contexts.Add(context);
            }

            [OnSerialized]
            internal void OnSerializedMethod(StreamingContext context)
            {
                History.Add("serialized");
                Contexts.Add(context);
            }

            [OnDeserializing]
            internal void OnDeserializingMethod(StreamingContext context)
            {
                History.Add("deserializing");
                Contexts.Add(context);
            }

            [OnDeserialized]
            internal void OnDeserializedMethod(StreamingContext context)
            {
                History.Add("deserialized");
                Contexts.Add(context);
            }

            void IDeserializationCallback.OnDeserialization(object sender) => History.Add("deserialization");
        }

        [Serializable]
        public struct SimpleISerializableStruct : System.Runtime.Serialization.ISerializable, IDeserializationCallback
        {
            private List<string> _history;
            private List<StreamingContext> _contexts;

            public SimpleISerializableStruct(SerializationInfo info, StreamingContext context)
            {
                _history = null;
                _contexts = null;
                Payload = info.GetString(nameof(Payload));
                History.Add("serialization_ctor");
                Contexts.Add(context);
            }

            public List<string> History => _history ??= new List<string>();
            public List<StreamingContext> Contexts => _contexts ??= new List<StreamingContext>();

            public string Payload { get; set; }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                Contexts.Add(context);
                info.AddValue(nameof(Payload), Payload);
            }

            [OnSerializing]
            internal void OnSerializingMethod(StreamingContext context)
            {
                History.Add("serializing");
                Contexts.Add(context);
            }

            [OnSerialized]
            internal void OnSerializedMethod(StreamingContext context)
            {
                History.Add("serialized");
                Contexts.Add(context);
            }

            [OnDeserializing]
            internal void OnDeserializingMethod(StreamingContext context)
            {
                History.Add("deserializing");
                Contexts.Add(context);
            }

            [OnDeserialized]
            internal void OnDeserializedMethod(StreamingContext context)
            {
                History.Add("deserialized");
                Contexts.Add(context);
            }

            void IDeserializationCallback.OnDeserialization(object sender) => History.Add("deserialization");
        }
    }
}