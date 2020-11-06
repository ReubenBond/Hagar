﻿using Hagar.Serializers;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class SortedListCodec<TKey, TValue> : GeneralizedReferenceTypeSurrogateCodec<SortedList<TKey, TValue>, SortedListSurrogate<TKey, TValue>>
    {
        public SortedListCodec(IValueSerializer<SortedListSurrogate<TKey, TValue>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override SortedList<TKey, TValue> ConvertFromSurrogate(ref SortedListSurrogate<TKey, TValue> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                SortedList<TKey, TValue> result;
                if (surrogate.Comparer is object)
                {
                    result = new SortedList<TKey, TValue>(surrogate.Comparer);
                }
                else
                {
                    result = new SortedList<TKey, TValue>();
                }

                foreach (var kvp in surrogate.Values)
                {
                    result.Add(kvp.Key, kvp.Value);
                }

                return result;
            }
        }

        public override void ConvertToSurrogate(SortedList<TKey, TValue> value, ref SortedListSurrogate<TKey, TValue> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new SortedListSurrogate<TKey, TValue>
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
    public struct SortedListSurrogate<TKey, TValue>
    {
        [Id(1)]
        public List<KeyValuePair<TKey, TValue>> Values { get; set; }

        [Id(2)]
        public IComparer<TKey> Comparer { get; set; }
    }
}
