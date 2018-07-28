using System;
using System.Collections.Generic;
using System.Text;
using Hagar.Buffers;
using Hagar.Utilities;

namespace Hagar.TypeSystem
{
    public class TypeCodec
    {
        private readonly CachedReadConcurrentDictionary<Type, TypeKey> typeCache = new CachedReadConcurrentDictionary<Type, TypeKey>();
        private readonly CachedReadConcurrentDictionary<TypeKey, Type> typeKeyCache =
            new CachedReadConcurrentDictionary<TypeKey, Type>(new TypeKey.Comparer());
        private readonly ITypeResolver typeResolver;
        private readonly Func<Type, TypeKey> getTypeKey;

        public TypeCodec(ITypeResolver typeResolver)
        {
            this.typeResolver = typeResolver;
            this.getTypeKey = type => new TypeKey(Encoding.UTF8.GetBytes(RuntimeTypeNameFormatter.Format(type)));
        }

        public void Write(Writer writer, Type type)
        {
            var key = this.typeCache.GetOrAdd(type, this.getTypeKey);
            writer.Write(key.HashCode);
            writer.WriteVarInt((uint)key.TypeName.Length);
            writer.Write(key.TypeName);
        }

        public bool TryRead(Reader reader, out Type type)
        {
            var key = ReadTypeKey(reader);

            if (this.typeKeyCache.TryGetValue(key, out type)) return type != null;

            this.typeResolver.TryResolveType(Encoding.UTF8.GetString(key.TypeName), out type);
            if (type != null)
            {
                this.typeKeyCache[key] = type;
            }

            return type != null;
        }

        public Type Read(Reader reader)
        {
            var key = ReadTypeKey(reader);

            if (this.typeKeyCache.TryGetValue(key, out var type)) return type;

            type = this.typeResolver.ResolveType(Encoding.UTF8.GetString(key.TypeName));
            if (type != null)
            {
                this.typeKeyCache[key] = type;
            }

            return type;
        }

        private static TypeKey ReadTypeKey(Reader reader)
        {
            var hashCode = reader.ReadInt32();
            var count = reader.ReadVarUInt32();
            var typeName = reader.ReadBytes((int) count);
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

            public TypeKey(string typeName) : this(Encoding.UTF8.GetBytes(typeName))
            {
            }

            public string GetTypeName() => Encoding.UTF8.GetString(this.TypeName);

            public bool Equals(TypeKey other)
            {
                if (this.HashCode != other.HashCode) return false;
                var a = this.TypeName;
                var b = other.TypeName;
                if (ReferenceEquals(a, b)) return true;
                if (a.Length != b.Length) return false;
                var length = a.Length;
                for (var i = 0; i < length; i++) if (a[i] != b[i]) return false;
                return true;
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