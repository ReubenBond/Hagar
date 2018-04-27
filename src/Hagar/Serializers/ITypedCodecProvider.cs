using Hagar.Codecs;

namespace Hagar.Serializers
{
    public interface ITypedCodecProvider
    {
        IFieldCodec<TField> GetCodec<TField>();
        IFieldCodec<TField> TryGetCodec<TField>();
    }
}