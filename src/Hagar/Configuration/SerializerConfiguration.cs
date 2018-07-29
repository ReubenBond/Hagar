using System;
using System.Collections.Generic;

namespace Hagar.Configuration
{
    public class SerializerConfiguration
    {
        public HashSet<Type> Activators { get; } = new HashSet<Type>();

        public HashSet<Type> FieldCodecs { get; } = new HashSet<Type>();

        public HashSet<Type> Serializers { get; } = new HashSet<Type>();

        public HashSet<Type> ValueSerializers { get; } = new HashSet<Type>();
    }

    public class TypeConfiguration
    {
        public Dictionary<uint, Type> WellKnownTypes { get; } = new Dictionary<uint, Type>();
    }
}