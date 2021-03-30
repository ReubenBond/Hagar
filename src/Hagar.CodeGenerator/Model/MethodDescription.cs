using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Hagar.CodeGenerator
{
    internal class MethodDescription
    {
        private readonly InvokableInterfaceDescription _iface;

        public MethodDescription(InvokableInterfaceDescription containingType, IMethodSymbol method, string name, bool hasCollision)
        {
            _iface = containingType;
            Method = method;
            Name = name;
            HasCollision = hasCollision;

            var names = new HashSet<string>(StringComparer.Ordinal);
            AllTypeParameters = new List<(string Name, ITypeParameterSymbol Parameter)>();
            MethodTypeParameters = new List<(string Name, ITypeParameterSymbol Parameter)>();

            foreach (var tp in _iface.InterfaceType.GetAllTypeParameters())
            {
                var tpName = GetTypeParameterName(names, tp);
                AllTypeParameters.Add((tpName, tp));
            }

            foreach (var tp in method.TypeParameters)
            {
                var tpName = GetTypeParameterName(names, tp);
                AllTypeParameters.Add((tpName, tp));
                MethodTypeParameters.Add((tpName, tp));
            }

#pragma warning disable RS1024 // Compare symbols correctly
            TypeParameterSubstitutions = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

            foreach (var tp in AllTypeParameters)
            {
                TypeParameterSubstitutions[tp.Parameter] = tp.Name;
            }

#pragma warning disable RS1024 // Compare symbols correctly
            InvokableBaseTypes = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

            // Set defaults from the interface type.
            foreach (var pair in containingType.InvokableBaseTypes)
            {
                InvokableBaseTypes[pair.Key] = pair.Value;
            }

            // Set overrides from user-defined attributes on the method.
            foreach (var methodAttr in method.GetAttributes())
            {
                if (!methodAttr.AttributeClass.GetAttributes(containingType.CodeGenerator.LibraryTypes.InvokableBaseTypeAttribute, out var attrs))
                {
                    continue;
                }

                foreach (var attr in attrs)
                {
                    var ctorArgs = attr.ConstructorArguments;
                    var proxyBaseType = (INamedTypeSymbol)ctorArgs[0].Value;
                    var returnType = (INamedTypeSymbol)ctorArgs[1].Value;
                    var invokableBaseType = (INamedTypeSymbol)ctorArgs[2].Value;
                    if (SymbolEqualityComparer.Default.Equals(containingType.ProxyBaseType, proxyBaseType))
                    {
                        // This attribute does not apply to this particular invoker, since it is for a different proxy base type.
                        continue;
                    }

                    InvokableBaseTypes[returnType] = invokableBaseType;
                }
            }

            static string GetTypeParameterName(HashSet<string> names, ITypeParameterSymbol tp)
            {
                var count = 0;
                var result = tp.Name;
                while (names.Contains(result))
                {
                    result = $"{tp.Name}_{++count}";
                }

                names.Add(result);
                return result.EscapeIdentifier();
            }
        }

        public string Name { get; }

        public IMethodSymbol Method { get; }

        public bool HasCollision { get; }

        public List<(string Name, ITypeParameterSymbol Parameter)> AllTypeParameters { get; }

        public List<(string Name, ITypeParameterSymbol Parameter)> MethodTypeParameters { get; }
        
        public Dictionary<ITypeParameterSymbol, string> TypeParameterSubstitutions { get; }

        /// <summary>
        /// Mapping of method return types to invokable base type. The code generator will create a derived type with the method arguments as fields.
        /// </summary>
        public Dictionary<INamedTypeSymbol, INamedTypeSymbol> InvokableBaseTypes { get; }

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Method);
    }
}