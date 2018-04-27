namespace Hagar.Serializers
{
    public interface IPartialSerializerProvider
    {
        IPartialSerializer<TField> GetPartialSerializer<TField>() where TField : class;
    }
}