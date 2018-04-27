using System;
using Hagar.Codecs;

namespace Hagar.Serializers
{
    public interface IUntypedCodecProvider
    {
        IFieldCodec<object> GetCodec(Type fieldType);
        IFieldCodec<object> TryGetCodec(Type fieldType);
    }

    public interface ITypedCodecProvider
    {
        IFieldCodec<TField> GetCodec<TField>();
        IFieldCodec<TField> TryGetCodec<TField>();
    }

    public interface IWrappedCodec
    {
        object InnerCodec { get; }
    }

    public interface IGeneralizedCodec : IFieldCodec<object>
    {
        bool IsSupportedType(Type type);
    }
}