using System.Diagnostics.CodeAnalysis;

namespace Hagar.Activators
{
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IActivator<T>
    {
        T Create();
    }

    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IActivator<TArgs, TResult>
    {
        TResult Create(TArgs arg);
    }
}
