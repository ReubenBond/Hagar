using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hagar.GeneratedCodeHelpers
{
    /// <summary>
    /// Utilities for use by generated code.
    /// </summary>
    public static class HagarGeneratedCodeHelper
    {
        private static readonly ThreadLocal<RecursiveServiceResolutionState> ResolutionState = new ThreadLocal<RecursiveServiceResolutionState>(() => new RecursiveServiceResolutionState());

        private sealed class RecursiveServiceResolutionState
        {
            private int _depth;

            public List<object> Callers { get; } = new List<object>();

            public void Enter(object caller)
            {
                ++_depth;
                if (caller is object)
                {
                    Callers.Add(caller);
                }
            }

            public void Exit()
            {
                if (--_depth <= 0)
                {
                    Callers.Clear();
                }
            }
        }

        /// <summary>
        /// Unwraps the provided service if it was wrapped.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="caller">The caller.</param>
        /// <param name="codecProvider">The codec provider.</param>
        /// <returns>The unwrapped service.</returns>
        public static TService GetService<TService>(object caller, ICodecProvider codecProvider)
        {
            var state = ResolutionState.Value;

            try
            {
                state.Enter(caller);


                foreach (var c in state.Callers)
                {
                    if (c is TService s && !(c is IServiceHolder<TService>))
                    {
                        return s;
                    }
                }

                return Unwrap(ActivatorUtilities.GetServiceOrCreateInstance<TService>(codecProvider.Services));
            }
            finally
            {
                state.Exit();
            }

            static TService Unwrap(TService val)
            {
                while (val is IServiceHolder<TService> wrapping)
                {
                    val = wrapping.Value;
                }

                return val;
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
            while (service is IServiceHolder<TService> && caller is TService callerService)
            {
                return callerService;
            }

            var state = ResolutionState.Value;

            try
            {
                state.Enter(caller);

                foreach (var c in state.Callers)
                {
                    if (c is TService s && !(c is IServiceHolder<TService>))
                    {
                        return s;
                    }
                }

                return Unwrap(service);
            }
            finally
            {
                state.Exit();
            }

            static TService Unwrap(TService val)
            {
                while (val is IServiceHolder<TService> wrapping)
                {
                    val = wrapping.Value;
                }

                return val;
            }
        }

        internal static object TryGetService(Type serviceType)
        {
            var state = ResolutionState.Value;
            foreach (var c in state.Callers)
            {
                var type = c?.GetType();
                if (serviceType == type)
                {
                    return c;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TArgument InvokableThrowArgumentOutOfRange<TArgument>(int index, int maxArgs) => throw new ArgumentOutOfRangeException($"The argument index value {index} must be between 0 and {maxArgs}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadHeader<TInput>(ref Reader<TInput> reader, ref Field header, int id)
        {
            reader.ReadFieldHeader(ref header);
            if (header.IsEndBaseOrEndObject)
            {
                return -1;
            }

            return (int)(id + header.FieldIdDelta);
        }

        /// <remarks>This method exists separate from <see cref="ReadHeader{TInput}(ref Reader{TInput}, ref Field, int)"/> in order to assist the CPU branch predictor.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadHeaderExpectingEndBaseOrEndObject<TInput>(ref Reader<TInput> reader, ref Field header, int id)
        {
            reader.ReadFieldHeader(ref header);
            if (header.IsEndBaseOrEndObject)
            {
                return -1;
            }

            return (int)(id + header.FieldIdDelta);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SerializeUnexpectedType<TBufferWriter, TField>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, TField value) where TBufferWriter : IBufferWriter<byte>
        {
            var specificSerializer = writer.Session.CodecProvider.GetCodec(value.GetType());
            specificSerializer.WriteField(ref writer, fieldIdDelta, expectedType, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TField DeserializeUnexpectedType<TInput, TField>(ref Reader<TInput> reader, Field field)
        {
            var specificSerializer = reader.Session.CodecProvider.GetCodec(field.FieldType);
            return (TField)specificSerializer.ReadValue(ref reader, field);
        }
    }
}