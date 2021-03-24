namespace Hagar.Serializers
{
    public interface IBaseCodecProvider
    {
        IBaseCodec<TField> GetBaseCodec<TField>() where TField : class;
    }
}