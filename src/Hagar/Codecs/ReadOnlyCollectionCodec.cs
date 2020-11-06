using Hagar.Serializers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hagar.Codecs
{
    [GenerateSerializer]
    public struct ReadOnlyCollectionSurrogate<T>
    {
        [Id(1)]
        public List<T> Values { get; set; }
    }

    [RegisterSerializer]
    public sealed class ReadOnlyCollectionCodec<T> : GeneralizedReferenceTypeSurrogateCodec<ReadOnlyCollection<T>, ReadOnlyCollectionSurrogate<T>>
    {
        public ReadOnlyCollectionCodec(IValueSerializer<ReadOnlyCollectionSurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ReadOnlyCollection<T> ConvertFromSurrogate(ref ReadOnlyCollectionSurrogate<T> surrogate) => surrogate.Values switch
        {
            object => new ReadOnlyCollection<T>(surrogate.Values),
            _ => null
        };

        public override void ConvertToSurrogate(ReadOnlyCollection<T> value, ref ReadOnlyCollectionSurrogate<T> surrogate)
        {
            switch (value)
            {
                case object:
                    surrogate = new ReadOnlyCollectionSurrogate<T>
                    {
                        Values = new List<T>(value)
                    };
                    break;
                default:
                    surrogate = default;
                    break;
            }
        }
    }
}