﻿using Hagar.Codecs;
using Hagar.Invocation;
using Hagar.Serializers;
using Hagar.WireProtocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hagar.UnitTests
{
    [DefaultInvokableBaseType(typeof(ValueTask<>), typeof(Request<>))]
    [DefaultInvokableBaseType(typeof(ValueTask), typeof(Request))]
    [DefaultInvokableBaseType(typeof(Task<>), typeof(TaskRequest<>))]
    [DefaultInvokableBaseType(typeof(Task), typeof(TaskRequest))]
    [DefaultInvokableBaseType(typeof(void), typeof(VoidRequest))]
    public abstract class MyInvokableProxyBase
    {
        protected void SendRequest(IResponseCompletionSource callback, IInvokable body)
        {
        }
    }

    [GenerateMethodSerializers(typeof(MyInvokableProxyBase))]
    public interface IMyInvokableBaseType { }

    public interface IG2<T1, T2> : IMyInvokableBaseType 
    { }

    public class HalfOpenGrain1<T> : IG2<T, int>
    { }
    public class HalfOpenGrain2<T> : IG2<int, T>
    { }

    public class OpenGeneric<T2, T1> : IG2<T2, T1>
    { }

    public class ClosedGeneric : IG2<Dummy1, Dummy2>
    { }

    public class ClosedGenericWithManyInterfaces : IG2<Dummy1, Dummy2>, IG2<Dummy2, Dummy1>
    { }

    public class Dummy1 { }

    public class Dummy2 { }

    public interface IG<T> : IMyInvokableBaseType 
    {
    }

    public class G1<T1, T2, T3, T4> : Root<T1>.IA<T2, T3, T4>
    {
    }

    public class Root<TRoot>
    {
        public interface IA<T1, T2, T3> : IMyInvokableBaseType 
        {

        }

        public class G<T1, T2, T3> : IG<IA<T1, T2, T3>>
        {
        }
    }

    public interface IGrainWithGenericMethods : IMyInvokableBaseType 
    {
        Task<Type[]> GetTypesExplicit<T, U, V>();
        Task<Type[]> GetTypesInferred<T, U, V>(T t, U u, V v);
        Task<Type[]> GetTypesInferred<T, U>(T t, U u, int v);
        Task<T> RoundTrip<T>(T val);
        Task<int> RoundTrip(int val);
        Task<T> Default<T>();
        Task<string> Default();
        Task<TGrain> Constraints<TGrain>(TGrain grain) where TGrain : IMyInvokableBaseType;
        ValueTask<int> ValueTaskMethod(bool useCache);
    }

    public class GrainWithGenericMethods : IGrainWithGenericMethods
    {
        private object state;

        public Task<Type[]> GetTypesExplicit<T, U, V>()
        {
            return Task.FromResult(new[] {typeof(T), typeof(U), typeof(V)});
        }

        public Task<Type[]> GetTypesInferred<T, U, V>(T t, U u, V v)
        {
            return Task.FromResult(new[] { typeof(T), typeof(U), typeof(V) });
        }

        public Task<Type[]> GetTypesInferred<T, U>(T t, U u, int v)
        {
            return Task.FromResult(new[] { typeof(T), typeof(U) });
        }

        public Task<T> RoundTrip<T>(T val)
        {
            return Task.FromResult(val);
        }

        public Task<int> RoundTrip(int val)
        {
            return Task.FromResult(-val);
        }

        public Task<T> Default<T>()
        {
            return Task.FromResult(default(T));
        }

        public Task<string> Default()
        {
            return Task.FromResult("default string");
        }

        public Task<TGrain> Constraints<TGrain>(TGrain grain) where TGrain : IMyInvokableBaseType 
        {
            return Task.FromResult(grain);
        }

        public void SetValue<T>(T value)
        {
            this.state = value;
        }

        public Task<T> GetValue<T>() => Task.FromResult((T) this.state);

        public ValueTask<int> ValueTaskMethod(bool useCache)
        {
            if (useCache)
            {
                return new ValueTask<int>(1);
            }

            return new ValueTask<int>(Task.FromResult(2));
        }
    }

    public interface IGenericGrainWithGenericMethods<T> : IMyInvokableBaseType 
    {
        Task<T> Method(T value);
#pragma warning disable 693
        Task<T> Method<T>(T value);
#pragma warning restore 693
    }

    public interface IRuntimeCodeGenGrain<T> : IMyInvokableBaseType
    {
        /// <summary>
        /// Sets and returns the grain's state.
        /// </summary>
        /// <param name="value">The new state.</param>
        /// <returns>The current state.</returns>
        Task<T> SetState(T value);

        /// <summary>
        /// Tests that code generation correctly handles methods with reserved keyword identifiers.
        /// </summary>
        /// <returns>The current state's event.</returns>
        Task<@event> @static();
    }

    [Serializable]
    [GenerateSerializer]
    public class GenericGrainState<T>
    {
        [Id(1)]
        public T @event { get; set; }
    }

    /// <summary>
    /// A class designed to test that code generation correctly handles reserved keywords.
    /// </summary>
    public class @event : IEquatable<@event>
    {
        private static readonly IEqualityComparer<@event> EventComparerInstance = new EventEqualityComparer();

        public enum @enum
        {
            @async,
            @int,
        }

        /// <summary>
        /// A public field.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// A private field.
        /// </summary>
        private Guid privateId;

        /// <summary>
        /// A property with a reserved keyword type and identifier.
        /// </summary>
        public @event @public { get; set; }

        /// <summary>
        /// Gets or sets the enum.
        /// </summary>
        public @enum Enum { get; set; }

        /// <summary>
        /// A property with a reserved keyword generic type and identifier.
        /// </summary>
        public List<@event> @if { get; set; }

        public static IEqualityComparer<@event> EventComparer
        {
            get
            {
                return EventComparerInstance;
            }
        }

        /// <summary>
        /// Gets or sets the private id.
        /// </summary>
        // ReSharper disable once ConvertToAutoProperty
        public Guid PrivateId
        {
            get
            {
                return this.privateId;
            }

            set
            {
                this.privateId = value;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((@event)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (this.@if != null ? this.@if.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.@public != null ? this.@public.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.privateId.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Id.GetHashCode();
                return hashCode;
            }
        }

        public bool Equals(@event other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (this.@if != other.@if)
            {
                if (this.@if != null && !this.@if.SequenceEqual(other.@if, EventComparer))
                {
                    return false;
                }
            }

            if (!Equals(this.@public, other.@public))
            {
                if (this.@public != null && !this.@public.Equals(other.@public))
                {
                    return false;
                }
            }

            return this.privateId.Equals(other.privateId) && this.Id.Equals(other.Id) && this.Enum == other.Enum;
        }

        private sealed class EventEqualityComparer : IEqualityComparer<@event>
        {
            public bool Equals(@event x, @event y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(@event obj)
            {
                return obj.GetHashCode();
            }
        }
    }

    public class NestedGeneric<T>
    {
        public Nested Payload { get; set; }

        public class Nested
        {
            public T Value { get; set; }
        }
    }

    public class NestedConstructedGeneric
    {
        public Nested<int> Payload { get; set; }

        public class Nested<T>
        {
            public T Value { get; set; }
        }
    }

    public interface INestedGenericGrain : IMyInvokableBaseType 
    {
        Task<int> Do(NestedGeneric<int> value);
        Task<int> Do(NestedConstructedGeneric value);
    }

    /// <summary>
    /// Tests that nested classes do not fail code generation.
    /// </summary>
    public class NestedGenericGrain : INestedGenericGrain
    {
        public Task<int> Do(NestedGeneric<int> value)
        {
            return Task.FromResult(value.Payload.Value);
        }

        public Task<int> Do(NestedConstructedGeneric value)
        {
            return Task.FromResult(value.Payload.Value);
        }
    }
}
