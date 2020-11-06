using Hagar.Codecs;
using Hagar.Codecs.SystemNet;
using Hagar.Serializers;
using Hagar.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
// ReSharper disable UnusedMember.Global

namespace Hagar.UnitTests
{
    [GenerateSerializer]
    public enum MyEnum : short
    {
        None,
        One,
        Two
    }

    public class EnumTests : FieldCodecTester<MyEnum, IFieldCodec<MyEnum>>
    {
        protected override IFieldCodec<MyEnum> CreateCodec() => ServiceProvider.GetRequiredService<ICodecProvider>().GetCodec<MyEnum>();
        protected override MyEnum CreateValue() => (MyEnum)(new Random(Guid.NewGuid().GetHashCode()).Next((int)MyEnum.None, (int)MyEnum.Two));
        protected override bool Equals(MyEnum left, MyEnum right) => left.Equals(right);
        protected override MyEnum[] TestValues => new[] { MyEnum.None, MyEnum.One, MyEnum.Two, (MyEnum)(-1), (MyEnum)10_000};
        protected override void Configure(IHagarBuilder builder)
        {
            ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.RemoveAll(typeof(IFieldCodec<MyEnum>)));
            builder.AddAssembly(typeof(EnumTests).Assembly);
        }
    }

    public class DayOfWeekTests : FieldCodecTester<DayOfWeek, IFieldCodec<DayOfWeek>>
    {
        protected override IFieldCodec<DayOfWeek> CreateCodec() => ServiceProvider.GetRequiredService<ICodecProvider>().GetCodec<DayOfWeek>();
        protected override DayOfWeek CreateValue() => (DayOfWeek)(new Random(Guid.NewGuid().GetHashCode()).Next((int)DayOfWeek.Sunday, (int)DayOfWeek.Saturday));
        protected override bool Equals(DayOfWeek left, DayOfWeek right) => left.Equals(right);
        protected override DayOfWeek[] TestValues => new[] { DayOfWeek.Monday, DayOfWeek.Sunday, (DayOfWeek)(-1), (DayOfWeek)10_000};
        protected override void Configure(IHagarBuilder builder) => ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.RemoveAll(typeof(IFieldCodec<DayOfWeek>)));
    }

    public class DateTimeTests : FieldCodecTester<DateTime, DateTimeCodec>
    {
        protected override DateTime CreateValue() => DateTime.UtcNow;
        protected override bool Equals(DateTime left, DateTime right) => left.Equals(right);
        protected override DateTime[] TestValues => new[] { DateTime.MinValue, DateTime.MaxValue, new DateTime(1970, 1, 1, 0, 0, 0) };
    }

    public class TimeSpanTests : FieldCodecTester<TimeSpan, TimeSpanCodec>
    {
        protected override TimeSpan CreateValue() => TimeSpan.FromMilliseconds(Guid.NewGuid().GetHashCode());
        protected override bool Equals(TimeSpan left, TimeSpan right) => left.Equals(right);
        protected override TimeSpan[] TestValues => new[] { TimeSpan.MinValue, TimeSpan.MaxValue, TimeSpan.Zero, TimeSpan.FromSeconds(12345) };
    }

    public class DateTimeOffsetTests : FieldCodecTester<DateTimeOffset, DateTimeOffsetCodec>
    {
        protected override DateTimeOffset CreateValue() => DateTime.UtcNow;
        protected override bool Equals(DateTimeOffset left, DateTimeOffset right) => left.Equals(right);

        protected override DateTimeOffset[] TestValues => new[]
        {
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue,
            new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0), TimeSpan.FromHours(11.5)),
            new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0), TimeSpan.FromHours(-11.5)),
        };
    }

    public class StringCodecTests : FieldCodecTester<string, StringCodec>
    {
        protected override string CreateValue() => Guid.NewGuid().ToString();
        protected override bool Equals(string left, string right) => StringComparer.Ordinal.Equals(left, right);
        protected override string[] TestValues => new[] { string.Empty, new string('*', 6), new string('x', 4097), "Hello, World!" };
    }

    public class ObjectCodecTests : FieldCodecTester<object, ObjectCodec>
    {
        protected override object CreateValue() => new object();
        protected override bool Equals(object left, object right) => ReferenceEquals(left, right) || typeof(object) == left?.GetType() && typeof(object) == right?.GetType();
        protected override object[] TestValues => new[] { null, new object() };
    }

    public class ByteArrayCodecTests : FieldCodecTester<byte[], ByteArrayCodec>
    {
        protected override byte[] CreateValue() => Guid.NewGuid().ToByteArray();

        protected override bool Equals(byte[] left, byte[] right) => ReferenceEquals(left, right) || left.SequenceEqual(right);

        protected override byte[][] TestValues => new[]
        {
            Array.Empty<byte>(), Enumerable.Range(0, 4097).Select(b => unchecked((byte)b)).ToArray(), CreateValue(),
        };
    }

    internal class ArrayCodecTests : FieldCodecTester<int[], ArrayCodec<int>>
    {
        protected override int[] CreateValue() => Enumerable.Range(0, new Random(Guid.NewGuid().GetHashCode()).Next(120) + 50).Select(_ => Guid.NewGuid().GetHashCode()).ToArray();
        protected override bool Equals(int[] left, int[] right) => ReferenceEquals(left, right) || left.SequenceEqual(right);
        protected override int[][] TestValues => new[] { CreateValue(), CreateValue(), CreateValue() };
    }

    public class UInt64CodecTests : FieldCodecTester<ulong, UInt64Codec>
    {
        protected override ulong CreateValue()
        {
            var msb = (ulong)Guid.NewGuid().GetHashCode() << 32;
            var lsb = (ulong)Guid.NewGuid().GetHashCode();
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
        protected override uint CreateValue() => (uint)Guid.NewGuid().GetHashCode();

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
        protected override ushort CreateValue() => (ushort)Guid.NewGuid().GetHashCode();
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
        protected override byte CreateValue() => (byte)Guid.NewGuid().GetHashCode();
        protected override byte[] TestValues => new byte[] { 0, 1, byte.MaxValue - 1, byte.MaxValue };
    }

    public class Int64CodecTests : FieldCodecTester<long, Int64Codec>
    {
        protected override long CreateValue()
        {
            var msb = (ulong)Guid.NewGuid().GetHashCode() << 32;
            var lsb = (ulong)Guid.NewGuid().GetHashCode();
            return (long)(msb | lsb);
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
        protected override short CreateValue() => (short)Guid.NewGuid().GetHashCode();

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
        protected override sbyte CreateValue() => (sbyte)Guid.NewGuid().GetHashCode();

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
        private int _createValueCount;
        protected override char CreateValue() => (char)('!' + _createValueCount++ % ('~' - '!'));
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
        private readonly Type[] _values =
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

        private int _valueIndex;

        protected override Type CreateValue() => _values[_valueIndex++ % _values.Length];
        protected override Type[] TestValues => _values;
    }

    public class FloatCodecTests : FieldCodecTester<float, FloatCodec>
    {
        protected override float CreateValue() => float.MaxValue * (float)new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
        protected override float[] TestValues => new[] { float.MinValue, 0, 1.0f, float.MaxValue };
    }

    public class DoubleCodecTests : FieldCodecTester<double, DoubleCodec>
    {
        protected override double CreateValue() => double.MaxValue * new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
        protected override double[] TestValues => new[] { double.MinValue, 0, 1.0, double.MaxValue };
    }

    public class DecimalCodecTests : FieldCodecTester<decimal, DecimalCodec>
    {
        protected override decimal CreateValue() => decimal.MaxValue * (decimal)new Random(Guid.NewGuid().GetHashCode()).NextDouble() * Math.Sign(Guid.NewGuid().GetHashCode());
        protected override decimal[] TestValues => new[] { decimal.MinValue, 0, 1.0M, decimal.MaxValue };
    }

    public class ListCodecTests : FieldCodecTester<List<int>, ListCodec<int>>
    {
        protected override List<int> CreateValue()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var result = new List<int>();
            for (var i = 0; i < rand.Next(17) + 5; i++)
            {
                result.Add(rand.Next());
            }

            return result;
        }

        protected override bool Equals(List<int> left, List<int> right) => object.ReferenceEquals(left, right) || left.SequenceEqual(right);
        protected override List<int>[] TestValues => new[] { CreateValue(), CreateValue(), CreateValue() };
    }

    public class DictionaryCodecTests : FieldCodecTester<Dictionary<string, int>, DictionaryCodec<string, int>>
    {
        protected override void Configure(IHagarBuilder builder) => _ = builder.AddISerializableSupport();

        protected override Dictionary<string, int> CreateValue()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var result = new Dictionary<string, int>();
            for (var i = 0; i < rand.Next(17) + 5; i++)
            {
                result[rand.Next().ToString()] = rand.Next();
            }

            return result;
        }

        protected override Dictionary<string, int>[] TestValues => new[] { CreateValue(), CreateValue(), CreateValue() };
        protected override bool Equals(Dictionary<string, int> left, Dictionary<string, int> right) => object.ReferenceEquals(left, right) || left.SequenceEqual(right);
    }

    public class IPAddressTests : FieldCodecTester<IPAddress, IPAddressCodec>
    {

        protected override void Configure(IHagarBuilder builder)
        {
            base.Configure(builder);
            builder.AddAssembly(typeof(IPAddressCodec).Assembly);
        }

        protected override IPAddress[] TestValues => new[] { IPAddress.Any, IPAddress.IPv6Any, IPAddress.IPv6Loopback, IPAddress.IPv6None, IPAddress.Loopback, IPAddress.Parse("123.123.10.3"), CreateValue() };

        protected override IPAddress CreateValue()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());
            byte[] bytes;
            if (rand.Next(1) == 0)
            {
                bytes = new byte[4];
            }
            else
            {
                bytes = new byte[16];
            }

            rand.NextBytes(bytes);
            return new IPAddress(bytes);
        } 
    }
}