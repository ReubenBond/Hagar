using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Hagar.Buffers;

namespace Hagar.TypeSystem
{
    public sealed class TypeCodec
    {
        private readonly ConcurrentDictionary<Type, TypeKey> typeCache = new ConcurrentDictionary<Type, TypeKey>();
        private readonly ConcurrentDictionary<int, (TypeKey Key, Type Type)> typeKeyCache = new ConcurrentDictionary<int, (TypeKey, Type)>();
        private readonly ITypeResolver typeResolver;
        private static readonly Func<Type, TypeKey> GetTypeKey = type => new TypeKey(Encoding.UTF8.GetBytes(RuntimeTypeNameFormatter.Format(type)));

        public TypeCodec(ITypeResolver typeResolver)
        {
            this.typeResolver = typeResolver;
        }

        public void Write<TBufferWriter>(ref Writer<TBufferWriter> writer, Type type) where TBufferWriter : IBufferWriter<byte>
        {
            var key = this.typeCache.GetOrAdd(type, GetTypeKey);
            writer.Write(key.HashCode);
            writer.WriteVarInt((uint)key.TypeName.Length);
            writer.Write(key.TypeName);
        }

        public unsafe bool TryRead(ref Reader reader, out Type type)
        {
            var hashCode = reader.ReadInt32();
            var count = (int)reader.ReadVarUInt32();

            if (!reader.TryReadBytes(count, out var typeName))
            {
                typeName = reader.ReadBytes((uint)count);
            }

            // Search through 
            var candidateHashCode = hashCode;
            while (this.typeKeyCache.TryGetValue(candidateHashCode, out var entry))
            {
                var existingKey = entry.Key;
                if (existingKey.HashCode != hashCode) break;

                var existingSpan = new ReadOnlySpan<byte>(existingKey.TypeName);
                if (existingSpan.SequenceEqual(typeName))
                {
                    type = entry.Type;
                    return true;
                }

                // Try the next entry.
                ++candidateHashCode;
            }

            // Allocate a string for the type name.
            string typeNameString;
            fixed (byte* typeNameBytes = typeName)
            {
                typeNameString = Encoding.UTF8.GetString(typeNameBytes, typeName.Length);
            }

            this.typeResolver.TryResolveType(typeNameString, out type);
            if (type is object)
            {
                var key = new TypeKey(hashCode, typeName.ToArray());
                while (!this.typeKeyCache.TryAdd(candidateHashCode++, (key, type)))
                {
                    // Insert the type at the first available position.
                }

                return true;
            }

            return false;
        }

        public unsafe Type Read(ref Reader reader)
        {
            var hashCode = reader.ReadInt32();
            var count = (int)reader.ReadVarUInt32();

            if (!reader.TryReadBytes(count, out var typeName))
            {
                typeName = reader.ReadBytes((uint)count);
            }

            // Search through 
            var candidateHashCode = hashCode;
            while (this.typeKeyCache.TryGetValue(candidateHashCode, out var entry))
            {
                var existingKey = entry.Key;
                if (existingKey.HashCode != hashCode) break;

                var existingSpan = new ReadOnlySpan<byte>(existingKey.TypeName);
                if (existingSpan.SequenceEqual(typeName)) return entry.Type;

                // Try the next entry.
                ++candidateHashCode;
            }

            // Allocate a string for the type name.
            string typeNameString;
            fixed (byte* typeNameBytes = typeName)
            {
                typeNameString = Encoding.UTF8.GetString(typeNameBytes, typeName.Length);
            }

            var type = this.typeResolver.ResolveType(typeNameString);
            if (type is object)
            {
                var key = new TypeKey(hashCode, typeName.ToArray());
                while (!this.typeKeyCache.TryAdd(candidateHashCode++, (key, type)))
                {
                    // Insert the type at the first available position.
                }
            }

            return type;
        }

        private static TypeKey ReadTypeKey(ref Reader reader)
        {
            var hashCode = reader.ReadInt32();
            var count = reader.ReadVarUInt32();
            var typeName = reader.ReadBytes(count);
            var key = new TypeKey(hashCode, typeName);
            return key;
        }

        /// <summary>
        /// Represents a named type for the purposes of serialization.
        /// </summary>
        internal struct TypeKey
        {
            public readonly int HashCode;

            public readonly byte[] TypeName;

            public TypeKey(int hashCode, byte[] key)
            {
                this.HashCode = hashCode;
                this.TypeName = key;
            }

            public TypeKey(byte[] key)
            {
                this.HashCode = unchecked((int) JenkinsHash.ComputeHash(key));
                this.TypeName = key;
            }

            public bool Equals(TypeKey other)
            {
                if (this.HashCode != other.HashCode) return false;
                var a = this.TypeName;
                var b = other.TypeName;
                return ReferenceEquals(a, b) || ByteArrayCompare(a, b);

                bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
                {
                    return a1.SequenceEqual(a2);
                }
            }

            public override bool Equals(object obj)
            {
                return obj is TypeKey key && this.Equals(key);
            }

            public override int GetHashCode()
            {
                return this.HashCode;
            }

            internal class Comparer : IEqualityComparer<TypeKey>
            {
                public bool Equals(TypeKey x, TypeKey y)
                {
                    return x.Equals(y);
                }

                public int GetHashCode(TypeKey obj)
                {
                    return obj.HashCode;
                }
            }
        }
    }
}