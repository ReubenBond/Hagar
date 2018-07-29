using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Hagar.Buffers;
using Hagar.Utilities;

namespace Hagar.TypeSystem
{
    public class TypeCodec
    {
        private readonly ConcurrentDictionary<Type, TypeKey> typeCache = new ConcurrentDictionary<Type, TypeKey>();
        private readonly ConcurrentDictionary<TypeKey, Type> typeKeyCache = new ConcurrentDictionary<TypeKey, Type>(new TypeKey.Comparer());
        private readonly ITypeResolver typeResolver;
        private static readonly Func<Type, TypeKey> GetTypeKey = type => new TypeKey(Encoding.UTF8.GetBytes(RuntimeTypeNameFormatter.Format(type)));

        public TypeCodec(ITypeResolver typeResolver)
        {
            this.typeResolver = typeResolver;
        }

        public void Write(ref Writer writer, Type type)
        {
            var key = this.typeCache.GetOrAdd(type, GetTypeKey);
            writer.Write(key.HashCode);
            writer.WriteVarInt((uint)key.TypeName.Length);
            writer.Write(key.TypeName);
        }

        public bool TryRead(ref Reader reader, out Type type)
        {
            var key = ReadTypeKey(ref reader);

            if (this.typeKeyCache.TryGetValue(key, out type)) return type != null;

            this.typeResolver.TryResolveType(Encoding.UTF8.GetString(key.TypeName), out type);
            if (type != null)
            {
                this.typeKeyCache[key] = type;
            }

            return type != null;
        }

        public Type Read(ref Reader reader)
        {
            var key = ReadTypeKey(ref reader);

            if (this.typeKeyCache.TryGetValue(key, out var type)) return type;

            type = this.typeResolver.ResolveType(Encoding.UTF8.GetString(key.TypeName));
            if (type != null)
            {
                this.typeKeyCache[key] = type;
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