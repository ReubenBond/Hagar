using Hagar.Codecs;
using System;

namespace Hagar.Serializers
{
    public interface IGeneralizedCodec : IFieldCodec<object>
    {
        bool IsSupportedType(Type type);
    }

    public interface ISpecializableCodec
    {
        bool IsSupportedType(Type type);
        IFieldCodec GetSpecializedCodec(Type type);
    }
}