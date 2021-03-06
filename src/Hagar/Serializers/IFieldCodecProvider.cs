using Hagar.Codecs;
using System;

namespace Hagar.Serializers
{
    public interface IFieldCodecProvider
    {
        IFieldCodec<TField> GetCodec<TField>();
        IFieldCodec<TField> TryGetCodec<TField>();
        IFieldCodec<object> GetCodec(Type fieldType);
        IFieldCodec<object> TryGetCodec(Type fieldType);
    }
}