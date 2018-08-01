﻿using System;
using System.IO.Pipelines;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Hagar.UnitTests
{
    [Trait("Category", "BVT")]
    public class GeneratedSerializerTests : IDisposable
    {
        private readonly ServiceProvider serviceProvider;
        private readonly ITypedCodecProvider codecProvider;
        private readonly SessionPool sessionPool;

        public GeneratedSerializerTests()
        {
            this.serviceProvider = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(GeneratedSerializerTests).Assembly)
                .BuildServiceProvider();
            this.codecProvider = this.serviceProvider.GetRequiredService<ITypedCodecProvider>();
            this.sessionPool = this.serviceProvider.GetRequiredService<SessionPool>();
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughCodec()
        {
            var original = new SomeClassWithSerialzers { IntField = 2, IntProperty = 30 };
            var result = this.RoundTripThroughCodec(original);

            Assert.Equal(original.IntField, result.IntField);
            Assert.Equal(original.IntProperty, result.IntProperty);
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer()
        {
            var original = new SomeClassWithSerialzers { IntField = 2, IntProperty = 30 };
            var result = (SomeClassWithSerialzers) this.RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original.IntField, result.IntField);
            Assert.Equal(original.IntProperty, result.IntProperty);
        }

        [Fact]
        public void UnmarkedFieldsAreNotSerialized()
        {
            var original = new SomeClassWithSerialzers { IntField = 2, IntProperty = 30, UnmarkedField = 12, UnmarkedProperty = 47 };
            var result = this.RoundTripThroughCodec(original);

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
            var result = (GenericPoco<string>) this.RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original.ArrayField, result.ArrayField);
            Assert.Equal(original.Field, result.Field);
        }

        [Fact]
        public void ArraysAreSupported()
        {
            var original = new[] { "a", "bb", "ccc" };
            var result = (string[])this.RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original, result);
        }

        [Fact]
        public void ArraysPocoRoundTrip()
        {
            var original = new ArrayPoco<int>
            {
                Array = new[] {1, 2, 3},
                Dim2 = new int[,] {{1}, {2}},
                Dim3 = new int[,,] {{{2}}},
                Dim4 = new int[,,,] { { { { 4} } } },
                Dim32 = new int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] {{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{809}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}},
                Jagged = new int[][] {new int[] {909}}
            };
            var result = (ArrayPoco<int>)this.RoundTripThroughUntypedSerializer(original);

            Assert.Equal(JsonConvert.SerializeObject(original), JsonConvert.SerializeObject(result));
        }

        [Fact]
        public void MultiDimensionalArraysAreSupported()
        {
            var array2d = new string[,] { { "1", "2", "3" }, { "4", "5", "6" }, { "7", "8", "9" } };
            var result2d = (string[,]) this.RoundTripThroughUntypedSerializer(array2d);

            Assert.Equal(array2d, result2d);
            var array3d = new string[,,]
            {
                { { "b", "b", "4" }, { "a", "g", "a" }, { "a", "g", "p" } },
                { { "g", "r", "g" }, { "1", "3", "a" }, { "l", "k", "a" } },
                { { "z", "b", "g" }, { "5", "7", "a" }, { "5", "n", "0" } }
            };
            var result3d = (string[,,])this.RoundTripThroughUntypedSerializer(array3d);

            Assert.Equal(array3d, result3d);
        }

        public void Dispose()
        {
            this.serviceProvider?.Dispose();
        }

        private T RoundTripThroughCodec<T>(T original)
        {
            T result;
            var pipe = new Pipe();
            var writer = new Writer<PipeWriter>(pipe.Writer);
            using (var readerSession = this.sessionPool.GetSession())
            using (var writeSession = this.sessionPool.GetSession())
            {
                var codec = this.codecProvider.GetCodec<T>();
                codec.WriteField(
                    ref writer,
                    writeSession,
                    0,
                    null,
                    original);
                writer.Commit();
                pipe.Writer.FlushAsync().GetAwaiter().GetResult();
                pipe.Writer.Complete();

                pipe.Reader.TryRead(out var readResult);
                var reader = new Reader(readResult.Buffer);

                var initialHeader = reader.ReadFieldHeader(readerSession);
                result = codec.ReadValue(ref reader, readerSession, initialHeader);
                pipe.Reader.AdvanceTo(readResult.Buffer.End);
                pipe.Reader.Complete();
            }

            return result;
        }

        private object RoundTripThroughUntypedSerializer(object original)
        {
            var pipe = new Pipe();
            object result;
            var writer = new Writer<PipeWriter>(pipe.Writer);
            using (var readerSession = this.sessionPool.GetSession())
            using (var writeSession = this.sessionPool.GetSession())
            {
                var serializer = this.serviceProvider.GetService<Serializer<object>>();
                serializer.Serialize(ref writer, writeSession, original);

                pipe.Writer.FlushAsync().GetAwaiter().GetResult();
                pipe.Writer.Complete();

                pipe.Reader.TryRead(out var readResult);
                var reader = new Reader(readResult.Buffer);

                result = serializer.Deserialize(ref reader, readerSession);
                pipe.Reader.AdvanceTo(readResult.Buffer.End);
                pipe.Reader.Complete();
            }

            return result;
        }
    }
}
