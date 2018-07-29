namespace Hagar.Serializers
{
    public interface IValueSerializerProvider
    {
        IValueSerializer<TField> GetValueSerializer<TField>() where TField : struct;
    }
}