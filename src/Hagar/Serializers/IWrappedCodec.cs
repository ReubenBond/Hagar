namespace Hagar.Serializers
{
    internal interface IWrappedCodec
    {
        object Inner { get; }
    }

    internal interface IServiceHolder<T>
    {
        T Value { get; }
    }
}