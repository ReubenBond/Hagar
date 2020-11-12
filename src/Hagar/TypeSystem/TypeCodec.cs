using Hagar.Buffers;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Hagar.TypeSystem
{
    public sealed class TypeCodec
    {
        private readonly ConcurrentDictionary<Type, TypeKey> _typeCache = new ConcurrentDictionary<Type, TypeKey>();
        private readonly ConcurrentDictionary<int, (TypeKey Key, Type Type)> _typeKeyCache = new ConcurrentDictionary<int, (TypeKey, Type)>();
        private readonly TypeConverter _typeConverter;
        private readonly Func<Type, TypeKey> _getTypeKey;

        public TypeCodec(TypeConverter typeConverter)
        {
            _typeConverter = typeConverter;
            _getTypeKey = type => new TypeKey(Encoding.UTF8.GetBytes(_typeConverter.Format(type)));
        }

        public void WriteLengthPrefixed<TBufferWriter>(ref Writer<TBufferWriter> writer, Type type) where TBufferWriter : IBufferWriter<byte>
        {
            var key = _typeCache.GetOrAdd(type, _getTypeKey);
            writer.WriteVarInt((uint)key.TypeName.Length);
            writer.Write(key.TypeName);
        }

        public void WriteEncodedType<TBufferWriter>(ref Writer<TBufferWriter> writer, Type type) where TBufferWriter : IBufferWriter<byte>
        {
            var key = _typeCache.GetOrAdd(type, _getTypeKey);
            writer.Write(key.HashCode);
            writer.WriteVarInt((uint)key.TypeName.Length);
            writer.Write(key.TypeName);
        }

        public unsafe bool TryRead<TInput>(ref Reader<TInput> reader, out Type type)
        {
            var hashCode = reader.ReadInt32();
            var count = (int)reader.ReadVarUInt32();

            if (!reader.TryReadBytes(count, out var typeName))
            {
                typeName = reader.ReadBytes((uint)count);
            }

            // Search through 
            var candidateHashCode = hashCode;
            while (_typeKeyCache.TryGetValue(candidateHashCode, out var entry))
            {
                var existingKey = entry.Key;
                if (existingKey.HashCode != hashCode)
                {
                    break;
                }

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

            _ = _typeConverter.TryParse(typeNameString, out type);
            if (type is object)
            {
                var key = new TypeKey(hashCode, typeName.ToArray());
                while (!_typeKeyCache.TryAdd(candidateHashCode++, (key, type)))
                {
                    // Insert the type at the first available position.
                }

                return true;
            }

            return false;
        }

        public unsafe Type ReadLengthPrefixed<TInput>(ref Reader<TInput> reader)
        {
            var count = (int)reader.ReadVarUInt32();

            if (!reader.TryReadBytes(count, out var typeName))
            {
                typeName = reader.ReadBytes((uint)count);
            }

            // Allocate a string for the type name.
            string typeNameString;
            fixed (byte* typeNameBytes = typeName)
            {
                typeNameString = Encoding.UTF8.GetString(typeNameBytes, typeName.Length);
            }

            var type = _typeConverter.Parse(typeNameString);
            return type;
        }

        private static TypeKey ReadTypeKey<TInput>(ref Reader<TInput> reader)
        {
            var hashCode = reader.ReadInt32();
            var count = reader.ReadVarUInt32();
            var typeName = reader.ReadBytes(count);
            var key = new TypeKey(hashCode, typeName);
            return key;
        }

        public unsafe bool TryReadForAnalysis<TInput>(ref Reader<TInput> reader, out Type type, out string typeString)
        {
            var hashCode = reader.ReadInt32();
            var count = (int)reader.ReadVarUInt32();

            if (!reader.TryReadBytes(count, out var typeName))
            {
                typeName = reader.ReadBytes((uint)count);
            }

            // Allocate a string for the type name.
            string typeNameString;
            fixed (byte* typeNameBytes = typeName)
            {
                typeNameString = Encoding.UTF8.GetString(typeNameBytes, count);
            }

            _ = _typeConverter.TryParse(typeNameString, out type);
            var key = new TypeKey(hashCode, typeName.ToArray());
            typeString = key.ToString();
            return type is object; 
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
                HashCode = hashCode;
                TypeName = key;
            }

            public TypeKey(byte[] key)
            {
                HashCode = unchecked((int)JenkinsHash.ComputeHash(key));
                TypeName = key;
            }

            public bool Equals(TypeKey other)
            {
                if (HashCode != other.HashCode)
                {
                    return false;
                }

                var a = TypeName;
                var b = other.TypeName;
                return ReferenceEquals(a, b) || ByteArrayCompare(a, b);

                static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
                {
                    return a1.SequenceEqual(a2);
                }
            }

            public override bool Equals(object obj) => obj is TypeKey key && Equals(key);

            public override int GetHashCode() => HashCode;

            public override string ToString() => $"TypeName \"{Encoding.UTF8.GetString(TypeName)}\" (hash {HashCode:X8})";

            internal class Comparer : IEqualityComparer<TypeKey>
            {
                public bool Equals(TypeKey x, TypeKey y) => x.Equals(y);

                public int GetHashCode(TypeKey obj) => obj.HashCode;
            }
        }
    }
}