using Hagar.Serializers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ConcurrentDictionaryCodec<TKey, TValue> : GeneralizedReferenceTypeSurrogateCodec<ConcurrentDictionary<TKey, TValue>, ConcurrentDictionarySurrogate<TKey, TValue>>
    {
        public ConcurrentDictionaryCodec(IValueSerializer<ConcurrentDictionarySurrogate<TKey, TValue>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ConcurrentDictionary<TKey, TValue> ConvertFromSurrogate(ref ConcurrentDictionarySurrogate<TKey, TValue> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                // Order of the key-value pairs in the return value may not match the order of the key-value pairs in the surrogate
                return new ConcurrentDictionary<TKey, TValue>(surrogate.Values);
            }
        }

        public override void ConvertToSurrogate(ConcurrentDictionary<TKey, TValue> value, ref ConcurrentDictionarySurrogate<TKey, TValue> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new ConcurrentDictionarySurrogate<TKey, TValue>
                {
                    Values = new Dictionary<TKey, TValue>(value)
                };
            }
        }
    }

    [GenerateSerializer]
    public struct ConcurrentDictionarySurrogate<TKey, TValue>
    {
        [Id(1)]
        public Dictionary<TKey, TValue> Values { get; set; }
    }
}
