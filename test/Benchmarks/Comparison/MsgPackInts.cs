using System;
using Hagar;
using ProtoBuf;
using ZeroFormatter;

namespace Benchmarks.Comparison
{
    [Serializable]
    [GenerateSerializer]
    [MessagePack.MessagePackObject]
    [ProtoContract]
    [ZeroFormattable]
    public class MsgPackInts
    {
        [Id(0)]
        [MessagePack.Key(0)]
        [Index(0)]
        [ProtoMember(1)]
        public virtual int MyProperty1 { get; set; }

        [Id(1)]
        [MessagePack.Key(1)]
        [Index(1)]
        [ProtoMember(2)]
        public virtual int MyProperty2 { get; set; }

        [Id(2)]
        [MessagePack.Key(2)]
        [Index(2)]
        [ProtoMember(3)]
        public virtual int MyProperty3 { get; set; }

        [Id(3)]
        [MessagePack.Key(3)]
        [Index(3)]
        [ProtoMember(4)]
        public virtual int MyProperty4 { get; set; }

        [Id(4)]
        [MessagePack.Key(4)]
        [Index(4)]
        [ProtoMember(5)]
        public virtual int MyProperty5 { get; set; }

        [Id(5)]
        [MessagePack.Key(5)]
        [Index(5)]
        [ProtoMember(6)]
        public virtual int MyProperty6 { get; set; }

        [Id(6)]
        [MessagePack.Key(6)]
        [Index(6)]
        [ProtoMember(7)]
        public virtual int MyProperty7 { get; set; }

        [Id(7)]
        [ProtoMember(8)]
        [MessagePack.Key(7)]
        [Index(7)]
        public virtual int MyProperty8 { get; set; }

        [Id(8)]
        [ProtoMember(9)]
        [MessagePack.Key(8)]
        [Index(8)]
        public virtual int MyProperty9 { get; set; }
    }
}