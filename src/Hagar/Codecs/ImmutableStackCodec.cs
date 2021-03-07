using Hagar.Cloning;
using Hagar.Serializers;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ImmutableStackCodec<T> : GeneralizedReferenceTypeSurrogateCodec<ImmutableStack<T>, ImmutableStackSurrogate<T>>
    {
        public ImmutableStackCodec(IValueSerializer<ImmutableStackSurrogate<T>> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ImmutableStack<T> ConvertFromSurrogate(ref ImmutableStackSurrogate<T> surrogate) => surrogate.Values switch
        {
            null => default,
            object => ImmutableStack.CreateRange(surrogate.Values)
        };

        public override void ConvertToSurrogate(ImmutableStack<T> value, ref ImmutableStackSurrogate<T> surrogate) => surrogate = value switch
        {
            null => default,
            _ => new ImmutableStackSurrogate<T>
            {
                Values = new List<T>(value)
            },
        };
    }

    [GenerateSerializer]
    public struct ImmutableStackSurrogate<T>
    {
        [Id(1)]
        public List<T> Values { get; set; }
    }

    [RegisterCopier]
    public sealed class ImmutableStackCopier<T> : IDeepCopier<ImmutableStack<T>>
    {
        public ImmutableStack<T> DeepCopy(ImmutableStack<T> input, CopyContext _) => input;
    }
}
