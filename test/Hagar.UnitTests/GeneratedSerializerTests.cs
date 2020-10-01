using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO.Pipelines;
using Xunit;

namespace Hagar.UnitTests
{
    [Trait("Category", "BVT")]
    public class GeneratedSerializerTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ITypedCodecProvider _codecProvider;
        private readonly SessionPool _sessionPool;

        public GeneratedSerializerTests()
        {
            _serviceProvider = new ServiceCollection()
                .AddHagar(hagar => hagar.AddAssembly(typeof(GeneratedSerializerTests).Assembly))
                .BuildServiceProvider();
            _codecProvider = _serviceProvider.GetRequiredService<ITypedCodecProvider>();
            _sessionPool = _serviceProvider.GetRequiredService<SessionPool>();
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughCodec()
        {
            var original = new SomeClassWithSerializers { IntField = 2, IntProperty = 30 };
            var result = RoundTripThroughCodec(original);

            Assert.Equal(original.IntField, result.IntField);
            Assert.Equal(original.IntProperty, result.IntProperty);
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer()
        {
            var original = new SomeClassWithSerializers { IntField = 2, IntProperty = 30 };
            var result = (SomeClassWithSerializers)RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original.IntField, result.IntField);
            Assert.Equal(original.IntProperty, result.IntProperty);
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer_ImmutableClass()
        {
            var original = new ImmutableClass(30, 2, 88, 99);
            var result = (ImmutableClass)RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original.GetIntField(), result.GetIntField());
            Assert.Equal(original.IntProperty, result.IntProperty);
            Assert.Equal(0, result.UnmarkedField);
            Assert.Equal(0, result.UnmarkedProperty);
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer_ImmutableStruct()
        {
            var original = new ImmutableStruct(30, 2);
            var result = (ImmutableStruct)RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original.GetIntField(), result.GetIntField());
            Assert.Equal(original.IntProperty, result.IntProperty);
        }

        [Fact]
        public void UnmarkedFieldsAreNotSerialized()
        {
            var original = new SomeClassWithSerializers { IntField = 2, IntProperty = 30, UnmarkedField = 12, UnmarkedProperty = 47 };
            var result = RoundTripThroughCodec(original);

            Assert.NotEqual(original.UnmarkedField, result.UnmarkedField);
            Assert.NotEqual(original.UnmarkedProperty, result.UnmarkedProperty);
        }

        [Fact]
        public void GenericPocosCanRoundTrip()
        {
            var original = new GenericPoco<string>
            {
                ArrayField = new[] { "a", "bb", "ccc" },
                Field = Guid.NewGuid().ToString("N")
            };
            var result = (GenericPoco<string>)RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original.ArrayField, result.ArrayField);
            Assert.Equal(original.Field, result.Field);
        }

        [Fact]
        public void ArraysAreSupported()
        {
            var original = new[] { "a", "bb", "ccc" };
            var result = (string[])RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original, result);
        }

        [Fact]
        public void ArraysPocoRoundTrip()
        {
            var original = new ArrayPoco<int>
            {
                Array = new[] { 1, 2, 3 },
                Dim2 = new int[,] { { 1 }, { 2 } },
                Dim3 = new int[,,] { { { 2 } } },
                Dim4 = new int[,,,] { { { { 4 } } } },
                Dim32 = new int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] { { { { { { { { { { { { { { { { { { { { { { { { { { { { { { { { 809 } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } } },
                Jagged = new int[][] { new int[] { 909 } }
            };
            var result = (ArrayPoco<int>)RoundTripThroughUntypedSerializer(original);

            Assert.Equal(JsonConvert.SerializeObject(original), JsonConvert.SerializeObject(result));
        }

        [Fact]
        public void MultiDimensionalArraysAreSupported()
        {
            var array2d = new string[,] { { "1", "2", "3" }, { "4", "5", "6" }, { "7", "8", "9" } };
            var result2d = (string[,])RoundTripThroughUntypedSerializer(array2d);

            Assert.Equal(array2d, result2d);
            var array3d = new string[,,]
            {
                { { "b", "b", "4" }, { "a", "g", "a" }, { "a", "g", "p" } },
                { { "g", "r", "g" }, { "1", "3", "a" }, { "l", "k", "a" } },
                { { "z", "b", "g" }, { "5", "7", "a" }, { "5", "n", "0" } }
            };
            var result3d = (string[,,])RoundTripThroughUntypedSerializer(array3d);

            Assert.Equal(array3d, result3d);
        }

        public void Dispose() => _serviceProvider?.Dispose();

        private T RoundTripThroughCodec<T>(T original)
        {
            T result;
            var pipe = new Pipe();
            using (var readerSession = _sessionPool.GetSession())
            using (var writeSession = _sessionPool.GetSession())
            {
                var writer = Writer.Create(pipe.Writer, writeSession);
                var codec = _codecProvider.GetCodec<T>();
                codec.WriteField(
                    ref writer,
                    0,
                    null,
                    original);
                writer.Commit();
                _ = pipe.Writer.FlushAsync().GetAwaiter().GetResult();
                pipe.Writer.Complete();

                _ = pipe.Reader.TryRead(out var readResult);
                var reader = Reader.Create(readResult.Buffer, readerSession);

                var previousPos = reader.Position;
                var initialHeader = reader.ReadFieldHeader();
                Assert.True(reader.Position > previousPos);

                result = codec.ReadValue(ref reader, initialHeader);
                pipe.Reader.AdvanceTo(readResult.Buffer.End);
                pipe.Reader.Complete();
            }

            return result;
        }

        private object RoundTripThroughUntypedSerializer(object original)
        {
            var pipe = new Pipe();
            object result;
            using (var readerSession = _sessionPool.GetSession())
            using (var writeSession = _sessionPool.GetSession())
            {
                var writer = Writer.Create(pipe.Writer, writeSession);
                var serializer = _serviceProvider.GetService<Serializer<object>>();
                serializer.Serialize(ref writer, original);

                _ = pipe.Writer.FlushAsync().GetAwaiter().GetResult();
                pipe.Writer.Complete();

                _ = pipe.Reader.TryRead(out var readResult);
                var reader = Reader.Create(readResult.Buffer, readerSession);

                result = serializer.Deserialize(ref reader);
                pipe.Reader.AdvanceTo(readResult.Buffer.End);
                pipe.Reader.Complete();
            }

            return result;
        }
    }
}