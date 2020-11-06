using Hagar.Serializers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ReadOnlyDictionaryCodec<TKey, TValue> : GeneralizedReferenceTypeSurrogateCodec<ReadOnlyDictionary<TKey, TValue>, ReadOnlyDictionarySurrogate<TKey, TValue>>
    {
        public ReadOnlyDictionaryCodec(IValueSerializer<ReadOnlyDictionarySurrogate<TKey, TValue>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ReadOnlyDictionary<TKey, TValue> ConvertFromSurrogate(ref ReadOnlyDictionarySurrogate<TKey, TValue> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                return new ReadOnlyDictionary<TKey, TValue>(surrogate.Values);
            }
        }

        public override void ConvertToSurrogate(ReadOnlyDictionary<TKey, TValue> value, ref ReadOnlyDictionarySurrogate<TKey, TValue> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new ReadOnlyDictionarySurrogate<TKey, TValue>
                {
                    Values = new Dictionary<TKey, TValue>(value)
                };
            }
        }
    }

    [GenerateSerializer]
    public struct ReadOnlyDictionarySurrogate<TKey, TValue>
    {
        [Id(1)]
        public Dictionary<TKey, TValue> Values { get; set; }
    }
}
