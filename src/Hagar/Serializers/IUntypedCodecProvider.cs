using Hagar.Codecs;
using System;

namespace Hagar.Serializers
{
    public interface IUntypedCodecProvider
    {
        IFieldCodec<object> GetCodec(Type fieldType);
        IFieldCodec<object> TryGetCodec(Type fieldType);
    }
}