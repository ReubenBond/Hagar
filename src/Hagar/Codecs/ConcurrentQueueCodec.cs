using Hagar.Serializers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    /// <summary>
    /// Codec for <see cref="ConcurrentQueue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    [RegisterSerializer]
    public sealed class ConcurrentQueueCodec<T> : GeneralizedReferenceTypeSurrogateCodec<ConcurrentQueue<T>, ConcurrentQueueSurrogate<T>>
    {
        public ConcurrentQueueCodec(IValueSerializer<ConcurrentQueueSurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ConcurrentQueue<T> ConvertFromSurrogate(ref ConcurrentQueueSurrogate<T> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                return new ConcurrentQueue<T>(surrogate.Values);
            }
        }

        public override void ConvertToSurrogate(ConcurrentQueue<T> value, ref ConcurrentQueueSurrogate<T> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new ConcurrentQueueSurrogate<T>
                {
                    Values = new Queue<T>(value)
                };
            }
        }
    }

    [GenerateSerializer]
    public struct ConcurrentQueueSurrogate<T>
    {
        [Id(1)]
        public Queue<T> Values { get; set; }
    }
}