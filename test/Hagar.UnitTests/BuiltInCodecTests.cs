using System;
using System.Collections.Generic;
using System.Linq;
using Hagar.Codecs;
using Hagar.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Hagar.UnitTests
{
    public class StringCodecTests : FieldCodecTester<string, StringCodec>
    {
        protected override string CreateValue() => Guid.NewGuid().ToString();
        protected override bool Equals(string left, string right) => StringComparer.Ordinal.Equals(left, right);
    }

    public class ByteArrayCodecTests : FieldCodecTester<byte[], ByteArrayCodec>
    {
        protected override byte[] CreateValue() => Guid.NewGuid().ToByteArray();

        protected override bool Equals(byte[] left, byte[] right) => left.SequenceEqual(right);
    }

    internal class ArrayCodecTests : FieldCodecTester<int[], ArrayCodec<int>>
    {
        protected override int[] CreateValue() => Enumerable.Range(0, new Random(Guid.NewGuid().GetHashCode()).Next(120)).Select(_ => Guid.NewGuid().GetHashCode()).ToArray();
        protected override bool Equals(int[] left, int[] right) => left.SequenceEqual(right);
    }

    public class UInt64CodecTests : FieldCodecTester<ulong, UInt64Codec>
    {
        protected override ulong CreateValue()
        {
            var msb = (ulong) Guid.NewGuid().GetHashCode() << 32;
            var lsb = (ulong) Guid.NewGuid().GetHashCode();
            return msb | lsb;
        }
    }

    public class UInt32CodecTests : FieldCodecTester<uint, UInt32Codec>
    {
        protected override uint CreateValue() => (uint) Guid.NewGuid().GetHashCode();
    }

    public class UInt16CodecTests : FieldCodecTester<ushort, UInt16Codec>
    {
        protected override ushort CreateValue() => (ushort) Guid.NewGuid().GetHashCode();
    }

    public class ByteCodecTests : FieldCodecTester<byte, ByteCodec>
    {
        protected override byte CreateValue() => (byte) Guid.NewGuid().GetHashCode();
    }

    public class Int64CodecTests : FieldCodecTester<long, Int64Codec>
    {
        protected override long CreateValue()
        {
            var msb = (ulong) Guid.NewGuid().GetHashCode() << 32;
            var lsb = (ulong) Guid.NewGuid().GetHashCode();
            return (long) (msb | lsb);
        }
    }

    public class Int32CodecTests : FieldCodecTester<int, Int32Codec>
    {
        protected override int CreateValue() => Guid.NewGuid().GetHashCode();
    }

    public class Int16CodecTests : FieldCodecTester<short, Int16Codec>
    {
        protected override short CreateValue() => (short) Guid.NewGuid().GetHashCode();
    }

    public class SByteCodecTests : FieldCodecTester<sbyte, SByteCodec>
    {
        protected override sbyte CreateValue() => (sbyte) Guid.NewGuid().GetHashCode();
    }

    public class CharCodecTests : FieldCodecTester<char, CharCodec>
    {
        private int createValueCount;
        protected override char CreateValue() => (char) ('!' + createValueCount++ % ('~' - '!'));
    }

    public class GuidCodecTests : FieldCodecTester<Guid, GuidCodec>
    {
        protected override Guid CreateValue() => Guid.NewGuid();
    }

    public class TypeCodecTests : FieldCodecTester<Type, TypeSerializerCodec>
    {
        private readonly Type[] values =
        {
            typeof(Dictionary<Guid, List<string>>),
            typeof(Type).MakeByRefType(),
            typeof(Guid),
            typeof(int).MakePointerType(),
            typeof(string[]),
            typeof(string[,]),
            typeof(string[,]).MakePointerType(),
            typeof(string[,]).MakeByRefType(),
            typeof(Dictionary<,>),
            typeof(List<>),
            typeof(string)
        };

        private int valueIndex;

        protected override Type CreateValue() => this.values[this.valueIndex++ % this.values.Length];
    }

    public class FloatCodecTests : FieldCodecTester<float, FloatCodec>
    {
        protected override float CreateValue() => float.MaxValue * (float) new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
    }

    public class DoubleCodecTests : FieldCodecTester<double, DoubleCodec>
    {
        protected override double CreateValue() => double.MaxValue * new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
    }

    public class DecimalCodecTests : FieldCodecTester<decimal, DecimalCodec>
    {
        protected override decimal CreateValue() => decimal.MaxValue * (decimal) new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
    }

    public class ListCodecTests : FieldCodecTester<List<int>, ListCodec<int>>
    {
        protected override List<int> CreateValue()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var result = new List<int>();
            for (var i = 0; i < rand.Next(17); i++) result.Add(rand.Next());
            return result;
        }

        protected override bool Equals(List<int> left, List<int> right) => left.SequenceEqual(right);
    }

    public class DictionaryCodecTests : FieldCodecTester<Dictionary<string, int>, DictionaryCodec<string, int>>
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddISerializableSupport();
        }

        protected override Dictionary<string, int> CreateValue()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var result = new Dictionary<string, int>();
            for (var i = 0; i < rand.Next(17); i++) result[rand.Next().ToString()] = rand.Next();
            return result;
        }

        protected override bool Equals(Dictionary<string, int> left, Dictionary<string, int> right) => left.SequenceEqual(right);
    }
}