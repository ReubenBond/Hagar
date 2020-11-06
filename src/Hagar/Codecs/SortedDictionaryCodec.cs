using Hagar.Serializers;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class SortedDictionaryCodec<TKey, TValue> : GeneralizedReferenceTypeSurrogateCodec<SortedDictionary<TKey, TValue>, SortedDictionarySurrogate<TKey, TValue>>
    {
        public SortedDictionaryCodec(IValueSerializer<SortedDictionarySurrogate<TKey, TValue>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override SortedDictionary<TKey, TValue> ConvertFromSurrogate(ref SortedDictionarySurrogate<TKey, TValue> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                SortedDictionary<TKey, TValue> result;
                if (surrogate.Comparer is object)
                {
                    result = new SortedDictionary<TKey, TValue>(surrogate.Comparer);
                }
                else
                {
                    result = new SortedDictionary<TKey, TValue>();
                }

                foreach (var kvp in surrogate.Values)
                {
                    result.Add(kvp.Key, kvp.Value);
                }

                return result;
            }
        }

        public override void ConvertToSurrogate(SortedDictionary<TKey, TValue> value, ref SortedDictionarySurrogate<TKey, TValue> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new SortedDictionarySurrogate<TKey, TValue>
                {
                    Values = new List<KeyValuePair<TKey, TValue>>(value)
                };

                if (!ReferenceEquals(value.Comparer, Comparer<TKey>.Default))
                {
                    surrogate.Comparer = value.Comparer;
                }
            }
        }
    }

    [GenerateSerializer]
    public struct SortedDictionarySurrogate<TKey, TValue>
    {
        [Id(1)]
        public List<KeyValuePair<TKey, TValue>> Values { get; set; }

        [Id(2)]
        public IComparer<TKey> Comparer { get; set; }
    }
}
