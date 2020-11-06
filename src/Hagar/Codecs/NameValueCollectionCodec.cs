using Hagar.Serializers;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Hagar.Codecs
{
    [RegisterSerializer]
    public sealed class NameValueCollectionCodec : GeneralizedReferenceTypeSurrogateCodec<NameValueCollection, NameValueCollectionSurrogate>
    {
        public NameValueCollectionCodec(IValueSerializer<NameValueCollectionSurrogate> surrogateSerializer) : base(surrogateSerializer)
        {
        }

        public override NameValueCollection ConvertFromSurrogate(ref NameValueCollectionSurrogate surrogate)
        {
            if (surrogate.Values is null)
            {
                return null;
            }
            else
            {
                var result = new NameValueCollection(surrogate.Values.Count);
                foreach (var value in surrogate.Values)
                {
                    result.Add(value.Key, value.Value);
                }

                return result;
            }
        }

        public override void ConvertToSurrogate(NameValueCollection value, ref NameValueCollectionSurrogate surrogate)
        {
            if (value is null)
            {
                surrogate = default;
                return;
            }
            else
            {
                surrogate = new NameValueCollectionSurrogate
                {
                    Values = new Dictionary<string, string>(),
                };
            }
        }
    }

    [GenerateSerializer]
    public struct NameValueCollectionSurrogate
    {
        [Id(1)]
        public Dictionary<string, string> Values { get; set; }
    }
}
