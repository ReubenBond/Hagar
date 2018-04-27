using System;
using Hagar.Codecs;

namespace Hagar.Serializers
{
    public interface IUntypedCodecProvider
    {
        IFieldCodec<object> GetCodec(Type fieldType);
        IFieldCodec<object> TryGetCodec(Type fieldType);
    }
}