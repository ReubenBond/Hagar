using System;
using Hagar.Codecs;

namespace Hagar.Serializers
{
    public interface IGeneralizedCodec : IFieldCodec<object>
    {
        bool IsSupportedType(Type type);
    }
}