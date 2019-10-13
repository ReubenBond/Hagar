using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hagar;

namespace TestRpc.Runtime
{
    [GenerateSerializer]
    public struct ActivationId
    {
        public bool Equals(ActivationId other) => this.Id == other.Id;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is ActivationId other && this.Equals(other);
        }

        public override int GetHashCode() => this.Id;

        public ActivationId(int id) => this.Id = id;

        [Id(0)]
        public int Id { get; set; }

        public static bool operator ==(ActivationId left, ActivationId right) => left.Id == right.Id;

        public static bool operator !=(ActivationId left, ActivationId right) => left.Id != right.Id;
    }

    public sealed class ActivationIdEqualityComparer : EqualityComparer<ActivationId>
    {
        public static ActivationIdEqualityComparer Instance { get; } = new ActivationIdEqualityComparer();
        public override bool Equals([AllowNull] ActivationId x, [AllowNull] ActivationId y) => x.Id == y.Id;

        public override int GetHashCode([DisallowNull] ActivationId obj) => obj.Id;
    }
}