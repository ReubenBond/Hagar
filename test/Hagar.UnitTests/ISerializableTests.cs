using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Hagar.Buffers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Hagar.UnitTests
{
    [Trait("Category", "BVT"), Trait("Category", "ISerializable")]
    public class ISerializableTests
    {
        private readonly IServiceProvider serviceProvider;
        private readonly SessionPool sessionPool;
        private readonly Serializer<object> serializer;

        public ISerializableTests()
        {
            var services = new ServiceCollection();
            services.AddHagar(hagar => hagar.AddISerializableSupport());

            this.serviceProvider = services.BuildServiceProvider();
            this.sessionPool = this.serviceProvider.GetService<SessionPool>();
            this.serializer = this.serviceProvider.GetRequiredService<Serializer<object>>();
        }

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

        private object SerializationLoop(object expected)
        {
            var pipe = new Pipe();

            using (var session = this.sessionPool.GetSession())
            {
                var writer = new Writer<PipeWriter>(pipe.Writer, session);
                this.serializer.Serialize(ref writer, expected);
                pipe.Writer.FlushAsync().GetAwaiter().GetResult();
                pipe.Writer.Complete();
            }

            using (var session = this.sessionPool.GetSession())
            {
                pipe.Reader.TryRead(out var readResult);
                var reader = new Reader(readResult.Buffer, session);
                var result = this.serializer.Deserialize(ref reader);
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

            var result2 = (SimpleISerializableObject) DotNetSerializationLoop(input2);

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
            private List<string> history;
            private List<StreamingContext> contexts;

            public SimpleISerializableObject()
            {
                this.History.Add("default_ctor");
            }

            public SimpleISerializableObject(SerializationInfo info, StreamingContext context)
            {
                this.History.Add("serialization_ctor");
                this.Contexts.Add(context);
                this.Payload = info.GetString(nameof(this.Payload));
            }

            public List<string> History => this.history ?? (this.history = new List<string>());
            public List<StreamingContext> Contexts => this.contexts ?? (this.contexts = new List<StreamingContext>());

            public string Payload { get; set; }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                this.Contexts.Add(context);
                info.AddValue(nameof(this.Payload), this.Payload);
            }

            [OnSerializing]
            internal void OnSerializingMethod(StreamingContext context)
            {
                this.History.Add("serializing");
                this.Contexts.Add(context);
            }

            [OnSerialized]
            internal void OnSerializedMethod(StreamingContext context)
            {
                this.History.Add("serialized");
                this.Contexts.Add(context);
            }

            [OnDeserializing]
            internal void OnDeserializingMethod(StreamingContext context)
            {
                this.History.Add("deserializing");
                this.Contexts.Add(context);
            }

            [OnDeserialized]
            internal void OnDeserializedMethod(StreamingContext context)
            {
                this.History.Add("deserialized");
                this.Contexts.Add(context);
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                this.History.Add("deserialization");
            }
        }

        [Serializable]
        public struct SimpleISerializableStruct : System.Runtime.Serialization.ISerializable, IDeserializationCallback
        {
            private List<string> history;
            private List<StreamingContext> contexts;

            public SimpleISerializableStruct(SerializationInfo info, StreamingContext context)
            {
                this.history = null;
                this.contexts = null;
                this.Payload = info.GetString(nameof(this.Payload));
                this.History.Add("serialization_ctor");
                this.Contexts.Add(context);
            }

            public List<string> History => this.history ?? (this.history = new List<string>());
            public List<StreamingContext> Contexts => this.contexts ?? (this.contexts = new List<StreamingContext>());

            public string Payload { get; set; }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                this.Contexts.Add(context);
                info.AddValue(nameof(this.Payload), this.Payload);
            }

            [OnSerializing]
            internal void OnSerializingMethod(StreamingContext context)
            {
                this.History.Add("serializing");
                this.Contexts.Add(context);
            }

            [OnSerialized]
            internal void OnSerializedMethod(StreamingContext context)
            {
                this.History.Add("serialized");
                this.Contexts.Add(context);
            }

            [OnDeserializing]
            internal void OnDeserializingMethod(StreamingContext context)
            {
                this.History.Add("deserializing");
                this.Contexts.Add(context);
            }

            [OnDeserialized]
            internal void OnDeserializedMethod(StreamingContext context)
            {
                this.History.Add("deserialized");
                this.Contexts.Add(context);
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                this.History.Add("deserialization");
            }
        }
    }
}
