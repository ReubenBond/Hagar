using Hagar.Serializers;
using System.Collections;
using System.Collections.Generic;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class ArrayListCodec : GeneralizedReferenceTypeSurrogateCodec<ArrayList, ArrayListSurrogate>
    {
        public ArrayListCodec(IValueSerializer<ArrayListSurrogate> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override ArrayList ConvertFromSurrogate(ref ArrayListSurrogate surrogate) => surrogate.Values switch
        {
            null => default,
            object => new ArrayList(surrogate.Values)
        };

        public override void ConvertToSurrogate(ArrayList value, ref ArrayListSurrogate surrogate)
        {
            if (value is null)
            {
                surrogate = default;
            }
            else
            {
                var result = new List<object>(value.Count);
                foreach (var item in value)
                {
                    result.Add(item);
                }

                surrogate = new ArrayListSurrogate
                {
                    Values = result
                };
            }
        }
    }

    [GenerateSerializer]
    public struct ArrayListSurrogate
    {
        [Id(1)]
        public List<object> Values { get; set; }
    }
}
