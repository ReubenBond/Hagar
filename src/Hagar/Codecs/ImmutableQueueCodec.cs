﻿using Hagar.Serializers;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ImmutableQueueCodec<T> : GeneralizedReferenceTypeSurrogateCodec<ImmutableQueue<T>, ImmutableQueueSurrogate<T>>
    {
        public ImmutableQueueCodec(IValueSerializer<ImmutableQueueSurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ImmutableQueue<T> ConvertFromSurrogate(ref ImmutableQueueSurrogate<T> surrogate) => surrogate.Values switch
        {
            null => null,
            object => ImmutableQueue.CreateRange<T>(surrogate.Values)
        };

        public override void ConvertToSurrogate(ImmutableQueue<T> value, ref ImmutableQueueSurrogate<T> surrogate) => surrogate = value switch
        {
            null => default, 
            object => new ImmutableQueueSurrogate<T>
            {
                Values = new List<T>(value)
            }
        };
    }

    [GenerateSerializer]
    public struct ImmutableQueueSurrogate<T>
    {
        [Id(1)]
        public List<T> Values { get; set; }
    }
}