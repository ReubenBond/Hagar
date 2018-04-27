using System;
using System.Collections.Generic;
using System.Threading;
using Hagar.Serializers;

namespace Hagar.GeneratedCodeHelpers
{
    /// <summary>
    /// Utilities for use by generated code.
    /// </summary>
    public static class HagarGeneratedCodeHelper
    {
        private static readonly ThreadLocal<RecursiveServiceResolutionState> ResolutionState = new ThreadLocal<RecursiveServiceResolutionState>(() => new RecursiveServiceResolutionState());

        private class RecursiveServiceResolutionState
        {
            private int depth;

            public List<object> Callers { get; } = new List<object>();

            public void Enter(object caller)
            {
                ++depth;
                Callers.Add(caller);
            }

            public void Exit()
            {
                if (--depth <= 0) this.Callers.Clear();
            }
        }

        /// <summary>
        /// Unwraps the provided service if it was wrapped.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="caller">The caller.</param>
        /// <param name="service">The service.</param>
        /// <returns>The unwrapped service.</returns>
        public static TService UnwrapService<TService>(object caller, TService service)
        {
            while (service is IServiceHolder<TService> && caller is TService callerService) return callerService;
            var state = ResolutionState.Value;
            
            try
            {
                state.Enter(caller);

                foreach (var c in state.Callers)
                {
                    if (c is TService s && !(c is IServiceHolder<TService>)) return s;
                }

                return Unwrap(service);
            }
            finally
            {
                state.Exit();
            }

            TService Unwrap(TService val)
            {
                while (val is IServiceHolder<TService> wrapping) val = wrapping.Value;
                return val;
            }
        }
    }
}