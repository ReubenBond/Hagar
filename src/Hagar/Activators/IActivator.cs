using System.Diagnostics.CodeAnalysis;

namespace Hagar.Activators
{
    public interface IActivator<T>
    {
        T Create();
    }

    public interface IActivator<TArgs, TResult>
    {
        TResult Create(TArgs arg);
    }
}