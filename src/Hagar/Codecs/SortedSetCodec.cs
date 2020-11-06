using Hagar.Serializers;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class SortedSetCodec<T> : GeneralizedReferenceTypeSurrogateCodec<SortedSet<T>, SortedSetSurrogate<T>>
    {
        public SortedSetCodec(IValueSerializer<SortedSetSurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override SortedSet<T> ConvertFromSurrogate(ref SortedSetSurrogate<T> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                if (surrogate.Comparer is object)
                {
                    return new SortedSet<T>(surrogate.Values, surrogate.Comparer);
                }
                else
                {
                    return new SortedSet<T>(surrogate.Values);
                }
            }
        }

        public override void ConvertToSurrogate(SortedSet<T> value, ref SortedSetSurrogate<T> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new SortedSetSurrogate<T>
                {
                    Values = new List<T>(value)
                };

                if (!ReferenceEquals(value.Comparer, Comparer<T>.Default))
                {
                    surrogate.Comparer = value.Comparer;
                }
            }
        }
    }

    [GenerateSerializer]
    public struct SortedSetSurrogate<T>
    {
        [Id(1)]
        public List<T> Values { get; set; }

        [Id(2)]
        public IComparer<T> Comparer { get; set; }
    }
}
