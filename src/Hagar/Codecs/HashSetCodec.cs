using Hagar.Cloning;
using Hagar.Serializers;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class HashSetCodec<T> : GeneralizedReferenceTypeSurrogateCodec<HashSet<T>, HashSetSurrogate<T>>
    {
        public HashSetCodec(IValueSerializer<HashSetSurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override HashSet<T> ConvertFromSurrogate(ref HashSetSurrogate<T> surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                if (surrogate.Comparer is object)
                {
                    return new HashSet<T>(surrogate.Values, surrogate.Comparer);
                }
                else
                {
                    return new HashSet<T>(surrogate.Values);
                }
            }
        }

        public override void ConvertToSurrogate(HashSet<T> value, ref HashSetSurrogate<T> surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new HashSetSurrogate<T>
                {
                    Values = new List<T>(value)
                };

                if (!ReferenceEquals(value.Comparer, EqualityComparer<T>.Default))
                {
                    surrogate.Comparer = value.Comparer;
                }
            }
        }
    }

    [GenerateSerializer]
    public struct HashSetSurrogate<T>
    {
        [Id(1)]
        public List<T> Values { get; set; }

        [Id(2)]
        public IEqualityComparer<T> Comparer { get; set; }
    }

    [RegisterCopier]
    public sealed class HashSetCopier<T> : IDeepCopier<HashSet<T>>, IBaseCopier<HashSet<T>>
    {
        private readonly IDeepCopier<T> _copier;

        public HashSetCopier(IDeepCopier<T> valueCopier)
        {
            _copier = valueCopier;
        }

        public HashSet<T> DeepCopy(HashSet<T> input, CopyContext context)
        {
            if (context.TryGetCopy<HashSet<T>>(input, out var result))
            {
                return result;
            }

            if (input.GetType() != typeof(HashSet<T>))
            {
                return context.Copy(input);
            }

            result = new HashSet<T>(input.Comparer);
            context.RecordCopy(input, result);
            foreach (var item in input)
            {
                result.Add(_copier.DeepCopy(item, context));
            }

            return result;
        }

        public void DeepCopy(HashSet<T> input, HashSet<T> output, CopyContext context)
        {
            foreach (var item in input)
            {
                output.Add(_copier.DeepCopy(item, context));
            }
        }
    }
}
