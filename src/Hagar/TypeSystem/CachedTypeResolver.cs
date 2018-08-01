﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Hagar.Utilities;

namespace Hagar.TypeSystem
{
    internal sealed class CachedTypeResolver : ITypeResolver
    {
        private readonly ConcurrentDictionary<string, Type> typeCache = new ConcurrentDictionary<string, Type>();
        private readonly CachedReadConcurrentDictionary<string, Assembly> assemblyCache = new CachedReadConcurrentDictionary<string, Assembly>();

        /// <inheritdoc />
        public Type ResolveType(string name)
        {
            if (this.TryResolveType(name, out var result)) return result;

            throw new TypeAccessException($"Unable to find a type named {name}");
        }

        /// <inheritdoc />
        public bool TryResolveType(string name, out Type type)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A FullName must not be null nor consist of only whitespace.", nameof(name));
            if (this.TryGetCachedType(name, out type)) return true;
            if (!this.TryPerformUncachedTypeResolution(name, out type)) return false;

            this.AddTypeToCache(name, type);
            return true;
        }

        private bool TryPerformUncachedTypeResolution(string name, out Type type)
        {
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (!this.TryPerformUncachedTypeResolution(name, out type, assemblies)) return false;

            if (type.Assembly.ReflectionOnly) throw new InvalidOperationException($"Type resolution for {name} yielded reflection-only type.");

            return true;
        }

        private bool TryGetCachedType(string name, out Type result)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("type name was null or whitespace");
            return this.typeCache.TryGetValue(name, out result);
        }

        private void AddTypeToCache(string name, Type type)
        {
            var entry = this.typeCache.GetOrAdd(name, _ => type);
            if (!ReferenceEquals(entry, type)) throw new InvalidOperationException("inconsistent type name association");
        }

        private bool TryPerformUncachedTypeResolution(string fullName, out Type type, IEnumerable<Assembly> assemblies)
        {
            if (null == assemblies) throw new ArgumentNullException(nameof(assemblies));
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("A type name must not be null nor consist of only whitespace.", nameof(fullName));

            foreach (var assembly in assemblies)
            {
                type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return true;
                }
            }

            type = Type.GetType(fullName, throwOnError: false) ?? Type.GetType(
                       fullName,
                       ResolveAssembly,
                       ResolveType,
                       false);
            return type != null;

            Assembly ResolveAssembly(AssemblyName assemblyName)
            {
                var fullAssemblyName = assemblyName.FullName;
                if (this.assemblyCache.TryGetValue(fullAssemblyName, out var result)) return result;

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var name = assembly.GetName();
                    this.assemblyCache[name.FullName] = assembly;
                    this.assemblyCache[name.Name] = assembly;
                }

                if (this.assemblyCache.TryGetValue(fullAssemblyName, out result)) return result;

                result = Assembly.Load(assemblyName);
                var resultName = result.GetName();
                this.assemblyCache[resultName.Name] = result;
                this.assemblyCache[resultName.FullName] = result;
                return result;
            }

            Type ResolveType(Assembly asm, string name, bool ignoreCase)
            {
                return asm?.GetType(name, throwOnError: false, ignoreCase: ignoreCase) ?? Type.GetType(name, throwOnError: false, ignoreCase: ignoreCase);
            }
        }
    }
}