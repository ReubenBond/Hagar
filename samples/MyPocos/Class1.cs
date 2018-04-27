using System;
using System.Collections.Generic;
using Hagar;

namespace MyPocos
{
    public class X
    {
        public void S()
        {
            throw new FieldAccessException();
        }
    }

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

        [FieldId(2222)]
        public Dictionary<T, T> DictField { get; set; }
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
}
