﻿using Hagar.Serializers;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ImmutableArrayCodec<T> : GeneralizedValueTypeSurrogateCodec<ImmutableArray<T>, ImmutableArraySurrogate<T>>
    {
        public ImmutableArrayCodec(IValueSerializer<ImmutableArraySurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ImmutableArray<T> ConvertFromSurrogate(ref ImmutableArraySurrogate<T> surrogate) => surrogate.Values switch
        {
            null => default,
            object => ImmutableArray.CreateRange(surrogate.Values)
        };

        public override void ConvertToSurrogate(ImmutableArray<T> value, ref ImmutableArraySurrogate<T> surrogate)
        {
            if (value.IsDefault)
            {
                surrogate = default;
            }
            else
            {
                surrogate = new ImmutableArraySurrogate<T>
                {
                    Values = new List<T>(value)
                };
            }
        }
    }

    [GenerateSerializer]
    public struct ImmutableArraySurrogate<T>
    {
        [Id(1)]
        public List<T> Values { get; set; }
    }
}
