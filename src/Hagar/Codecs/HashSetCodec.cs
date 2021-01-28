using Hagar.Serializers;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class HashSetCodec<T> : GeneralizedReferenceTypeSurrogateCodec<HashSet<T>, HashSetSurrogate<T>>
    {
        public HashSetCodec(IValueSerializer<HashSetSurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override HashSet<T> ConvertFromSurrogate(ref HashSetSurrogate<T> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                if (surrogate.Comparer is object)
                {
                    return new HashSet<T>(surrogate.Values, surrogate.Comparer);
                }
                else
                {
                    return new HashSet<T>(surrogate.Values);
                }
            }
        }

        public override void ConvertToSurrogate(HashSet<T> value, ref HashSetSurrogate<T> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new HashSetSurrogate<T>
                {
                    Values = new List<T>(value)
                };

                if (!ReferenceEquals(value.Comparer, EqualityComparer<T>.Default))
                {
                    surrogate.Comparer = value.Comparer;
                }
            }
        }
    }

    [GenerateSerializer]
    public struct HashSetSurrogate<T>
    {
        [Id(1)]
        public List<T> Values { get; set; }

        [Id(2)]
        public IEqualityComparer<T> Comparer { get; set; }
    }
}
