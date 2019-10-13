using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hagar;

namespace TestRpc.Runtime
{
    [GenerateSerializer]
    public struct GrainId
    {
        public bool Equals(GrainId other) => this.Id == other.Id;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is GrainId other && this.Equals(other);
        }

        public override int GetHashCode() => this.Id;

        public GrainId(int id) => this.Id = id;

        [Id(0)]
        public int Id { get; set; }

        public static bool operator ==(GrainId left, GrainId right) => left.Id == right.Id;

        public static bool operator !=(GrainId left, GrainId right) => left.Id != right.Id;
    }

    public sealed class GrainIdEqualityComparer : EqualityComparer<GrainId>
    {
        public static GrainIdEqualityComparer Instance { get; } = new GrainIdEqualityComparer();
        public override bool Equals([AllowNull] GrainId x, [AllowNull] GrainId y) => x.Id == y.Id;

        public override int GetHashCode([DisallowNull] GrainId obj) => obj.Id;
    }
}