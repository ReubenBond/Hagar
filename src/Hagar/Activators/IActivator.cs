using System.Diagnostics.CodeAnalysis;

namespace Hagar.Activators
{
    public interface IActivator<T>
    {
        T Create();
    }

    public interface IActivator<TArg1, TResult>
    {
        TResult Create(TArg1 arg);
    }

    public interface IActivator<TArg1, TArg2, TResult>
    {
        TResult Create(TArg1 arg1, TArg2 arg2);
    }

    public interface IActivator<TArg1, TArg2, TArg3, TResult>
    {
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3);
    }

    public interface IActivator<TArg1, TArg2, TArg3, TArg4, TResult>
    {
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
    }

    public interface IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>
    {
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
    }

    public interface IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>
    {
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6);
    }

    public interface IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>
    {
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7);
    }

    public interface IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>
    {
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8);
    }
}
