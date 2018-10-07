using System;
using System.Collections.Generic;
using System.Linq;
using Hagar.Codecs;
using Hagar.TestKit;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable UnusedMember.Global

namespace Hagar.UnitTests
{
    public class StringCodecTests : FieldCodecTester<string, StringCodec>
    {
        protected override string CreateValue() => Guid.NewGuid().ToString();
        protected override bool Equals(string left, string right) => StringComparer.Ordinal.Equals(left, right);
        protected override string[] TestValues => new[] { string.Empty, new string('*', 6), new string('x', 4097), "Hello, World!" };
    }

    public class ByteArrayCodecTests : FieldCodecTester<byte[], ByteArrayCodec>
    {
        protected override byte[] CreateValue() => Guid.NewGuid().ToByteArray();

        protected override bool Equals(byte[] left, byte[] right) => ReferenceEquals(left, right) || left.SequenceEqual(right);

        protected override byte[][] TestValues => new[]
        {
            Array.Empty<byte>(), Enumerable.Range(0, 4097).Select(b => unchecked((byte)b)).ToArray(), this.CreateValue(),
        };
    }

    internal class ArrayCodecTests : FieldCodecTester<int[], ArrayCodec<int>>
    {
        protected override int[] CreateValue() => Enumerable.Range(0, new Random(Guid.NewGuid().GetHashCode()).Next(120) + 50).Select(_ => Guid.NewGuid().GetHashCode()).ToArray();
        protected override bool Equals(int[] left, int[] right) => ReferenceEquals(left, right) || left.SequenceEqual(right);
        protected override int[][] TestValues => new[] { this.CreateValue(), this.CreateValue(), this.CreateValue() };
    }

    public class UInt64CodecTests : FieldCodecTester<ulong, UInt64Codec>
    {
        protected override ulong CreateValue()
        {
            var msb = (ulong) Guid.NewGuid().GetHashCode() << 32;
            var lsb = (ulong) Guid.NewGuid().GetHashCode();
            return msb | lsb;
        }

        protected override ulong[] TestValues => new ulong[]
        {
            0,
            1,
            (ulong)byte.MaxValue - 1,
            byte.MaxValue,
            (ulong)byte.MaxValue + 1,
            (ulong)ushort.MaxValue - 1,
            ushort.MaxValue,
            (ulong)ushort.MaxValue + 1,
            (ulong)uint.MaxValue - 1,
            uint.MaxValue,
            (ulong)uint.MaxValue + 1,
            ulong.MaxValue,
        };
    }

    public class UInt32CodecTests : FieldCodecTester<uint, UInt32Codec>
    {
        protected override uint CreateValue() => (uint) Guid.NewGuid().GetHashCode();

        protected override uint[] TestValues => new uint[]
        {
            0,
            1,
            (uint)byte.MaxValue - 1,
            byte.MaxValue,
            (uint)byte.MaxValue + 1,
            (uint)ushort.MaxValue - 1,
            ushort.MaxValue,
            (uint)ushort.MaxValue + 1,
            uint.MaxValue,
        };
    }

    public class UInt16CodecTests : FieldCodecTester<ushort, UInt16Codec>
    {
        protected override ushort CreateValue() => (ushort) Guid.NewGuid().GetHashCode();
        protected override ushort[] TestValues => new ushort[]
        {
            0,
            1,
            byte.MaxValue - 1,
            byte.MaxValue,
            byte.MaxValue + 1,
            ushort.MaxValue - 1,
            ushort.MaxValue,
        };
    }

    public class ByteCodecTests : FieldCodecTester<byte, ByteCodec>
    {
        protected override byte CreateValue() => (byte) Guid.NewGuid().GetHashCode();
        protected override byte[] TestValues => new byte[] { 0, 1, byte.MaxValue - 1, byte.MaxValue };
    }

    public class Int64CodecTests : FieldCodecTester<long, Int64Codec>
    {
        protected override long CreateValue()
        {
            var msb = (ulong) Guid.NewGuid().GetHashCode() << 32;
            var lsb = (ulong) Guid.NewGuid().GetHashCode();
            return (long) (msb | lsb);
        }

        protected override long[] TestValues => new[]
        {
            long.MinValue,
            -1,
            0,
            1,
            (long)sbyte.MaxValue - 1,
            sbyte.MaxValue,
            (long)sbyte.MaxValue + 1,
            (long)short.MaxValue - 1,
            short.MaxValue,
            (long)short.MaxValue + 1,
            (long)int.MaxValue - 1,
            int.MaxValue,
            (long)int.MaxValue + 1,
            long.MaxValue,
        };
    }

    public class Int32CodecTests : FieldCodecTester<int, Int32Codec>
    {
        protected override int CreateValue() => Guid.NewGuid().GetHashCode();

        protected override int[] TestValues => new[]
        {
            int.MinValue,
            -1,
            0,
            1,
            sbyte.MaxValue - 1,
            sbyte.MaxValue,
            sbyte.MaxValue + 1,
            short.MaxValue - 1,
            short.MaxValue,
            short.MaxValue + 1,
            int.MaxValue - 1,
            int.MaxValue,
        };
    }

    public class Int16CodecTests : FieldCodecTester<short, Int16Codec>
    {
        protected override short CreateValue() => (short) Guid.NewGuid().GetHashCode();

        protected override short[] TestValues => new short[]
        {
            short.MinValue,
            -1,
            0,
            1,
            sbyte.MaxValue - 1,
            sbyte.MaxValue,
            sbyte.MaxValue + 1,
            short.MaxValue - 1,
            short.MaxValue
        };
    }

    public class SByteCodecTests : FieldCodecTester<sbyte, SByteCodec>
    {
        protected override sbyte CreateValue() => (sbyte) Guid.NewGuid().GetHashCode();

        protected override sbyte[] TestValues => new sbyte[]
        {
            sbyte.MinValue,
            -1,
            0,
            1,
            sbyte.MaxValue - 1,
            sbyte.MaxValue
        };
    }

    public class CharCodecTests : FieldCodecTester<char, CharCodec>
    {
        private int createValueCount;
        protected override char CreateValue() => (char) ('!' + this.createValueCount++ % ('~' - '!'));
        protected override char[] TestValues => new[]
        {
            (char)0,
            (char)1,
            (char)(byte.MaxValue - 1),
            (char)byte.MaxValue,
            (char)(byte.MaxValue + 1),
            (char)(ushort.MaxValue - 1),
            (char)ushort.MaxValue,
        };
    }

    public class GuidCodecTests : FieldCodecTester<Guid, GuidCodec>
    {
        protected override Guid CreateValue() => Guid.NewGuid();
        protected override Guid[] TestValues => new[]
        {
            Guid.Empty,
            Guid.Parse("4DEBD074-5DBB-45F6-ACB7-ED97D2AEE02F"),
            Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
        };
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
        protected override Type[] TestValues => this.values;
    }

    public class FloatCodecTests : FieldCodecTester<float, FloatCodec>
    {
        protected override float CreateValue() => float.MaxValue * (float) new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
        protected override float[] TestValues => new[] { float.MinValue, 0, 1.0f, float.MaxValue };
    }

    public class DoubleCodecTests : FieldCodecTester<double, DoubleCodec>
    {
        protected override double CreateValue() => double.MaxValue * new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
        protected override double[] TestValues => new[] { double.MinValue, 0, 1.0, double.MaxValue };
    }

    public class DecimalCodecTests : FieldCodecTester<decimal, DecimalCodec>
    {
        protected override decimal CreateValue() => decimal.MaxValue * (decimal) new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
        protected override decimal[] TestValues => new[] { decimal.MinValue, 0, 1.0M, decimal.MaxValue };
    }

    public class ListCodecTests : FieldCodecTester<List<int>, ListCodec<int>>
    {
        protected override List<int> CreateValue()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var result = new List<int>();
            for (var i = 0; i < rand.Next(17) + 5; i++) result.Add(rand.Next());
            return result;
        }

        protected override bool Equals(List<int> left, List<int> right) => object.ReferenceEquals(left, right) || left.SequenceEqual(right);
        protected override List<int>[] TestValues => new[] { this.CreateValue(), this.CreateValue(), this.CreateValue() };
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
            for (var i = 0; i < rand.Next(17) + 5; i++) result[rand.Next().ToString()] = rand.Next();
            return result;
        }

        protected override Dictionary<string, int>[] TestValues => new[] { this.CreateValue(), this.CreateValue(), this.CreateValue() };
        protected override bool Equals(Dictionary<string, int> left, Dictionary<string, int> right) => object.ReferenceEquals(left, right) || left.SequenceEqual(right);
    }
}