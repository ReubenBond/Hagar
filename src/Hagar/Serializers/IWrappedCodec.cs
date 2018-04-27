namespace Hagar.Serializers
{
    internal interface IWrappedCodec
    {
        object InnerCodec { get; }
    }

    internal interface IServiceHolder<T>
    {
        T Value { get; }
    }
}