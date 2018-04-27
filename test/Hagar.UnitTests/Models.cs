using System.Collections.Generic;

namespace Hagar.UnitTests
{
    [GenerateSerializer]
    public class SomeClassWithSerialzers
    {
        [FieldId(0)]
        public int IntProperty { get; set; }

        [FieldId(1)] public int IntField;

        public int UnmarkedField;

        public int UnmarkedProperty { get; set; }

        public override string ToString()
        {
            return $"{nameof(this.IntField)}: {this.IntField}, {nameof(this.IntProperty)}: {this.IntProperty}";
        }
    }

    [GenerateSerializer]
    public class SerializableClassWithCompiledBase : List<int>
    {
        [FieldId(0)]
        public int IntProperty { get; set; }
    }

    [GenerateSerializer]
    public class GenericPoco<T>
    {
        [FieldId(0)]
        public T Field { get; set; }

        [FieldId(1030)]
        public T[] ArrayField { get; set; }
    }

    [GenerateSerializer]
    public class GenericPocoWithConstraint<TClass, TStruct>
        : GenericPoco<TStruct> where TClass : List<int>, new() where TStruct : struct
    {
        [FieldId(0)]
        public new TClass Field { get; set; }

        [FieldId(999)]
        public TStruct ValueField { get; set; }
    }

    [GenerateSerializer]
    public class ArrayPoco<T>
    {
        [FieldId(0)]
        public T[] Array { get; set; }

        [FieldId(1)]
        public T[,] Dim2 { get; set; }

        [FieldId(2)]
        public T[,,] Dim3 { get; set; }

        [FieldId(3)]
        public T[,,,] Dim4 { get; set; }
        
        [FieldId(4)]
        public T[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] Dim32 { get; set; }

        [FieldId(5)]
        public T[][] Jagged { get; set; }
    }
}
