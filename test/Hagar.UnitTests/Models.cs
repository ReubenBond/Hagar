using System.Collections.Generic;

namespace Hagar.UnitTests
{
    [GenerateSerializer]
    public class SomeClassWithSerializers
    {
        [Id(0)]
        public int IntProperty { get; set; }

        [Id(1)] public int IntField;

        public int UnmarkedField;

        public int UnmarkedProperty { get; set; }

        public override string ToString() => $"{nameof(IntField)}: {IntField}, {nameof(IntProperty)}: {IntProperty}";
    }

    [GenerateSerializer]
    public class SerializableClassWithCompiledBase : List<int>
    {
        [Id(0)]
        public int IntProperty { get; set; }
    }

    [GenerateSerializer]
    public class GenericPoco<T>
    {
        [Id(0)]
        public T Field { get; set; }

        [Id(1030)]
        public T[] ArrayField { get; set; }
    }

    [GenerateSerializer]
    public class GenericPocoWithConstraint<TClass, TStruct>
        : GenericPoco<TStruct> where TClass : List<int>, new() where TStruct : struct
    {
        [Id(0)]
        public new TClass Field { get; set; }

        [Id(999)]
        public TStruct ValueField { get; set; }
    }

    [GenerateSerializer]
    public class ArrayPoco<T>
    {
        [Id(0)]
        public T[] Array { get; set; }

        [Id(1)]
        public T[,] Dim2 { get; set; }

        [Id(2)]
        public T[,,] Dim3 { get; set; }

        [Id(3)]
        public T[,,,] Dim4 { get; set; }

        [Id(4)]
        public T[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] Dim32 { get; set; }

        [Id(5)]
        public T[][] Jagged { get; set; }
    }

    [GenerateSerializer]
    public class ImmutableClass
    {
        public ImmutableClass(int intProperty, int intField, int unmarkedField, int unmarkedProperty)
        {
            IntProperty = intProperty;
            _intField = intField;
            UnmarkedField = unmarkedField;
            UnmarkedProperty = unmarkedProperty;
        }

        [Id(0)]
        public int IntProperty { get; }

        [Id(1)] private readonly int _intField;

        public int GetIntField() => _intField;

        public readonly int UnmarkedField;

        public int UnmarkedProperty { get; }

        public override string ToString() => $"{nameof(_intField)}: {_intField}, {nameof(IntProperty)}: {IntProperty}";
    }

    [GenerateSerializer]
    public struct ImmutableStruct
    {
        public ImmutableStruct(int intProperty, int intField)
        {
            IntProperty = intProperty;
            _intField = intField;
        }

        [Id(0)]
        public int IntProperty { get; }

        [Id(1)] private readonly int _intField;
        public int GetIntField() => _intField;

        public override string ToString() => $"{nameof(_intField)}: {_intField}, {nameof(IntProperty)}: {IntProperty}";
    }
}