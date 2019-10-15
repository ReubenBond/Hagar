using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Hagar.Activators
{
    public class DefaultActivator<T> : IActivator<T>
    {
        private static readonly Func<T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                if (ctor.GetParameters().Length != 0) continue;

                var newExpression = Expression.New(ctor);
                DefaultConstructorFunction = Expression.Lambda<Func<T>>(newExpression).Compile();
                break;
            }
        }

        public T Create() => DefaultConstructorFunction != null ? DefaultConstructorFunction() : CreateUnformatted();

        private static T CreateUnformatted() => (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
    }

    public class DefaultActivator<TArg1, T> : IActivator<TArg1, T>
    {
        private static readonly Func<TArg1, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 1) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)})");
    }

    public class DefaultActivator<TArg1, TArg2, T> : IActivator<TArg1, TArg2, T>
    {
        private static readonly Func<TArg1, TArg2, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 2) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;
                if (!parameters[1].ParameterType.Equals(typeof(TArg2))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, TArg2, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1, TArg2 arg2) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1, arg2) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)}, {typeof(TArg2)})");
    }

    public class DefaultActivator<TArg1, TArg2, TArg3, T> : IActivator<TArg1, TArg2, TArg3, T>
    {
        private static readonly Func<TArg1, TArg2, TArg3, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 3) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;
                if (!parameters[1].ParameterType.Equals(typeof(TArg2))) continue;
                if (!parameters[2].ParameterType.Equals(typeof(TArg3))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, TArg2, TArg3, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1, TArg2 arg2, TArg3 arg3) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1, arg2, arg3) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)}, {typeof(TArg2)}, {typeof(TArg3)})");
    }

    public class DefaultActivator<TArg1, TArg2, TArg3, TArg4, T> : IActivator<TArg1, TArg2, TArg3, TArg4, T>
    {
        private static readonly Func<TArg1, TArg2, TArg3, TArg4, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 4) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;
                if (!parameters[1].ParameterType.Equals(typeof(TArg2))) continue;
                if (!parameters[2].ParameterType.Equals(typeof(TArg3))) continue;
                if (!parameters[3].ParameterType.Equals(typeof(TArg4))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, TArg2, TArg3, TArg4, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1, arg2, arg3, arg4) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)}, {typeof(TArg2)}, {typeof(TArg3)}, {typeof(TArg4)})");
    }

    public class DefaultActivator<TArg1, TArg2, TArg3, TArg4, TArg5, T> : IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, T>
    {
        private static readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 5) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;
                if (!parameters[1].ParameterType.Equals(typeof(TArg2))) continue;
                if (!parameters[2].ParameterType.Equals(typeof(TArg3))) continue;
                if (!parameters[3].ParameterType.Equals(typeof(TArg4))) continue;
                if (!parameters[4].ParameterType.Equals(typeof(TArg5))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, TArg2, TArg3, TArg4, TArg5, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1, arg2, arg3, arg4, arg5) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)}, {typeof(TArg2)}, {typeof(TArg3)}, {typeof(TArg4)}, {typeof(TArg5)})");
    }

    public class DefaultActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, T> : IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, T>
    {
        private static readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 6) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;
                if (!parameters[1].ParameterType.Equals(typeof(TArg2))) continue;
                if (!parameters[2].ParameterType.Equals(typeof(TArg3))) continue;
                if (!parameters[3].ParameterType.Equals(typeof(TArg4))) continue;
                if (!parameters[4].ParameterType.Equals(typeof(TArg5))) continue;
                if (!parameters[5].ParameterType.Equals(typeof(TArg6))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1, arg2, arg3, arg4, arg5, arg6) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)}, {typeof(TArg2)}, {typeof(TArg3)}, {typeof(TArg4)}, {typeof(TArg5)}, {typeof(TArg6)})");
    }

    public class DefaultActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, T> : IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, T>
    {
        private static readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 7) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;
                if (!parameters[1].ParameterType.Equals(typeof(TArg2))) continue;
                if (!parameters[2].ParameterType.Equals(typeof(TArg3))) continue;
                if (!parameters[3].ParameterType.Equals(typeof(TArg4))) continue;
                if (!parameters[4].ParameterType.Equals(typeof(TArg5))) continue;
                if (!parameters[5].ParameterType.Equals(typeof(TArg6))) continue;
                if (!parameters[6].ParameterType.Equals(typeof(TArg7))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)}, {typeof(TArg2)}, {typeof(TArg3)}, {typeof(TArg4)}, {typeof(TArg5)}, {typeof(TArg6)}, {typeof(TArg7)})");
    }

    public class DefaultActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, T> : IActivator<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, T>
    {
        private static readonly Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, T> DefaultConstructorFunction;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != 8) continue;
                if (!parameters[0].ParameterType.Equals(typeof(TArg1))) continue;
                if (!parameters[1].ParameterType.Equals(typeof(TArg2))) continue;
                if (!parameters[2].ParameterType.Equals(typeof(TArg3))) continue;
                if (!parameters[3].ParameterType.Equals(typeof(TArg4))) continue;
                if (!parameters[4].ParameterType.Equals(typeof(TArg5))) continue;
                if (!parameters[5].ParameterType.Equals(typeof(TArg6))) continue;
                if (!parameters[6].ParameterType.Equals(typeof(TArg7))) continue;
                if (!parameters[7].ParameterType.Equals(typeof(TArg8))) continue;

                DefaultConstructorFunction = Expression.Lambda<Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, T>>(Expression.New(ctor)).Compile();
                break;
            }
        }

        public T Create(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8) => DefaultConstructorFunction != null ? DefaultConstructorFunction(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) : ThrowConstructorNotFound();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowConstructorNotFound() => throw new InvalidOperationException($"A matching constructor was not found for signature {typeof(T)}({typeof(TArg1)}, {typeof(TArg2)}, {typeof(TArg3)}, {typeof(TArg4)}, {typeof(TArg5)}, {typeof(TArg6)}, {typeof(TArg7)}, {typeof(TArg8)})");
    }
}