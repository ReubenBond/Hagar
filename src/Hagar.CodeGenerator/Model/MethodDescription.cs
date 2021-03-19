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

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Method);
    }
}