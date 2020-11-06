using Hagar.Serializers;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ImmutableDictionaryCodec<TKey, TValue> : GeneralizedReferenceTypeSurrogateCodec<ImmutableDictionary<TKey, TValue>, ImmutableDictionarySurrogate<TKey, TValue>>
    {
        public ImmutableDictionaryCodec(IValueSerializer<ImmutableDictionarySurrogate<TKey, TValue>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ImmutableDictionary<TKey, TValue> ConvertFromSurrogate(ref ImmutableDictionarySurrogate<TKey, TValue> surrogate) => surrogate.Values switch
        {
            null => default,
            object => ImmutableDictionary.CreateRange(surrogate.Values)
        };

        public override void ConvertToSurrogate(ImmutableDictionary<TKey, TValue> value, ref ImmutableDictionarySurrogate<TKey, TValue> surrogate) => surrogate = value switch
        {
            null => default,
            _ => new ImmutableDictionarySurrogate<TKey, TValue>
            {
                Values = new Dictionary<TKey, TValue>(value)
            },
        };
    }

    [GenerateSerializer]
    public struct ImmutableDictionarySurrogate<TKey, TValue>
    {
        [Id(1)]
        public Dictionary<TKey, TValue> Values { get; set; }
    }
}
