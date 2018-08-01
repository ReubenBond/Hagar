using System;
using Hagar;
using MessagePack;
using ProtoBuf;
using ZeroFormatter;

namespace Benchmarks.Models
{
    [Serializable]
    [GenerateSerializer]
    [ProtoContract]
    [ZeroFormattable]
    [MessagePackObject]
    public struct IntStruct
    {
        public static IntStruct Create()
        {
            var result = new IntStruct();
            result.Initialize();
            return result;
        }

        public void Initialize() => this.MyProperty1 = this.MyProperty2 =
            this.MyProperty3 = this.MyProperty4 = this.MyProperty5 = this.MyProperty6 = this.MyProperty7 = this.MyProperty8 = this.MyProperty9 = 10;

        public IntStruct(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8, int p9)
        {
            this.MyProperty1 = p1;
            this.MyProperty2 = p2;
            this.MyProperty3 = p3;
            this.MyProperty4 = p4;
            this.MyProperty5 = p5;
            this.MyProperty6 = p6;
            this.MyProperty7 = p7;
            this.MyProperty8 = p8;
            this.MyProperty9 = p9;
        }

        [Id(0)]
        [Index(0)]
        [Key(0)]
        [ProtoMember(1)]
        public int MyProperty1 { get; set; }

        [Id(1)]
        [Index(1)]
        [Key(1)]
        [ProtoMember(2)]
        public int MyProperty2 { get; set; }

        [Id(2)]
        [Index(2)]
        [Key(2)]
        [ProtoMember(3)]
        public int MyProperty3 { get; set; }

        [Id(3)]
        [Index(3)]
        [Key(3)]
        [ProtoMember(4)]
        public int MyProperty4 { get; set; }

        [Id(4)]
        [Key(4)]
        [Index(4)]
        [ProtoMember(5)]
        public int MyProperty5 { get; set; }

        [Id(5)]
        [Key(5)]
        [Index(5)]
        [ProtoMember(6)]
        public int MyProperty6 { get; set; }

        [Id(6)]
        [Index(6)]
        [Key(6)]
        [ProtoMember(7)]
        public int MyProperty7 { get; set; }

        [Id(7)]
        [ProtoMember(8)]
        [Index(7)]
        [Key(7)]
        public int MyProperty8 { get; set; }

        [Id(8)]
        [ProtoMember(9)]
        [Index(8)]
        [Key(8)]
        public int MyProperty9 { get; set; }
    }
}