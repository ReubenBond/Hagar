using System;
using Hagar;
using ProtoBuf;
using ZeroFormatter;

namespace Benchmarks.Comparison
{
    [Serializable]
    [GenerateSerializer]
    [ProtoContract]
    [ZeroFormattable]
    public struct IntStruct
    {
        [Id(0)]
        [Index(0)]
        [ProtoMember(1)]
        public int MyProperty1 { get; set; }

        [Id(1)]
        [Index(1)]
        [ProtoMember(2)]
        public int MyProperty2 { get; set; }

        [Id(2)]
        [Index(2)]
        [ProtoMember(3)]
        public int MyProperty3 { get; set; }

        [Id(3)]
        [Index(3)]
        [ProtoMember(4)]
        public int MyProperty4 { get; set; }

        [Id(4)]
        [MessagePack.Key(4)]
        [Index(4)]
        [ProtoMember(5)]
        public int MyProperty5 { get; set; }

        [Id(5)]
        [MessagePack.Key(5)]
        [Index(5)]
        [ProtoMember(6)]
        public int MyProperty6 { get; set; }

        [Id(6)]
        [Index(6)]
        [ProtoMember(7)]
        public int MyProperty7 { get; set; }

        [Id(7)]
        [ProtoMember(8)]
        [Index(7)]
        public int MyProperty8 { get; set; }

        [Id(8)]
        [ProtoMember(9)]
        [Index(8)]
        public int MyProperty9 { get; set; }
    }
}