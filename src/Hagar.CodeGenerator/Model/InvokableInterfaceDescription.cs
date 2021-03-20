using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Hagar.CodeGenerator
{
    internal class InvokableInterfaceDescription : IInvokableInterfaceDescription
    {
        private readonly CodeGenerator _generator;

        public InvokableInterfaceDescription(
            CodeGenerator generator,
            SemanticModel semanticModel,
            INamedTypeSymbol interfaceType,
            string name,
            INamedTypeSymbol proxyBaseType,
            bool isExtension)
        {
            ValidateBaseClass(generator.LibraryTypes, proxyBaseType);
            _generator = generator;
            SemanticModel = semanticModel;
            InterfaceType = interfaceType;
            ProxyBaseType = proxyBaseType;
            IsExtension = isExtension;
            Name = name;
            GeneratedNamespace = CodeGenerator.CodeGeneratorName + "." + InterfaceType.GetNamespaceAndNesting();

            var names = new HashSet<string>(StringComparer.Ordinal);
            TypeParameters = new List<(string Name, ITypeParameterSymbol Parameter)>();

            foreach (var tp in interfaceType.GetAllTypeParameters())
            {
                var tpName = GetTypeParameterName(names, tp);
                TypeParameters.Add((tpName, tp));
            }

            Methods = GetMethods(interfaceType).ToList();

            static string GetTypeParameterName(HashSet<string> names, ITypeParameterSymbol tp)
            {
                var count = 0;
                var result = tp.Name;
                while (names.Contains(result))
                {
                    result = $"{tp.Name}_{++count}";
                }

                names.Add(result);
                return result;
            }
        }

        private IEnumerable<MethodDescription> GetMethods(INamedTypeSymbol symbol)
        {
#pragma warning disable RS1024 // Compare symbols correctly
            var methods = new Dictionary<IMethodSymbol, bool>(MethodSignatureComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
            foreach (var iface in GetAllInterfaces(symbol))
            {
                foreach (var method in iface.GetDeclaredInstanceMembers<IMethodSymbol>())
                {
                    if (methods.TryGetValue(method, out var description))
                    {
                        methods[method] = true;
                        continue;
                    }

                    methods.Add(method, false);
                }
            }

            var idCounter = 1;
            foreach (var pair in methods.OrderBy(kv => kv.Key, MethodSignatureComparer.Default))
            {
                var method = pair.Key;
                var id = _generator.GetId(method) ?? idCounter;
                if (id >= idCounter)
                {
                    idCounter = id + 1;
                }

                yield return new MethodDescription(this, method, id.ToString(CultureInfo.InvariantCulture), hasCollision: pair.Value);
            }

            IEnumerable<INamedTypeSymbol> GetAllInterfaces(INamedTypeSymbol s)
            {
                if (s.TypeKind == TypeKind.Interface)
                {
                    yield return s;
                }

                foreach (var i in s.AllInterfaces)
                {
                    yield return i;
                }
            }
        }

        public string Name { get; }
        public INamedTypeSymbol InterfaceType { get; }
        public List<MethodDescription> Methods { get; }
        public INamedTypeSymbol ProxyBaseType { get; }
        public bool IsExtension { get; }
        public SemanticModel SemanticModel { get; }
        public string GeneratedNamespace { get; }
        public List<(string Name, ITypeParameterSymbol Parameter)> TypeParameters { get; }

        private static void ValidateBaseClass(LibraryTypes l, INamedTypeSymbol baseClass)
        {
            var found = false;
            foreach (var member in baseClass.GetMembers("SendRequest"))
            {
                if (member is not IMethodSymbol method)
                {
                    Throw(member, "not method");
                }

                if (method.TypeParameters.Length != 0)
                {
                    Throw(member, "type params");
                }

                if (method.Parameters.Length != 2)
                {
                    Throw(member, "params length");
                }

                if (!SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, l.IResponseCompletionSource))
                {
                    Throw(member, "param 0");
                }

                if (!SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, l.IInvokable))
                {
                    Throw(member, "param 1");
                }

                if (!method.ReturnsVoid)
                {
                    Throw(member, "return type");
                }

                found = true;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"Proxy base class {baseClass} does not contain a definition for void SendRequest(IResponseCompletionSource, IInvokable)");
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Throw(ISymbol m, string x) => throw new InvalidOperationException("Complaint: " + x + " for symbol: " + m.ToDisplayString());
        }

        private sealed class MethodSignatureComparer : IEqualityComparer<IMethodSymbol>, IComparer<IMethodSymbol>
        {
            public static MethodSignatureComparer Default { get; } = new();

            private MethodSignatureComparer()
            {
            }

            public bool Equals(IMethodSymbol x, IMethodSymbol y)
            {
                if (!string.Equals(x.Name, y.Name, StringComparison.Ordinal))
                {
                    return false;
                }

                if (x.TypeArguments.Length != y.TypeArguments.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.TypeArguments.Length; i++)
                {
                    if (!SymbolEqualityComparer.Default.Equals(x.TypeArguments[i], y.TypeArguments[i]))
                    {
                        return false;
                    }
                }

                if (x.Parameters.Length != y.Parameters.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.Parameters.Length; i++)
                {
                    if (!SymbolEqualityComparer.Default.Equals(x.Parameters[i].Type, y.Parameters[i].Type))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(IMethodSymbol obj)
            {
                int hashCode = -499943048;
                hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(obj.Name);

                foreach (var arg in obj.TypeArguments)
                {
                    hashCode = hashCode * -1521134295 + SymbolEqualityComparer.Default.GetHashCode(arg);
                }

                foreach (var parameter in obj.Parameters)
                {
                    hashCode = hashCode * -1521134295 + SymbolEqualityComparer.Default.GetHashCode(parameter.Type);
                }

                return hashCode;
            }

            public int Compare(IMethodSymbol x, IMethodSymbol y)
            {
                var result = StringComparer.Ordinal.Compare(x.Name, y.Name);
                if (result != 0)
                {
                    return result;
                }

                result = x.TypeArguments.Length.CompareTo(y.TypeArguments.Length);
                if (result != 0)
                {
                    return result;
                }

                for (var i = 0; i < x.TypeArguments.Length; i++)
                {
                    var xh = SymbolEqualityComparer.Default.GetHashCode(x.TypeArguments[i]);
                    var yh = SymbolEqualityComparer.Default.GetHashCode(y.TypeArguments[i]);
                    result = xh.CompareTo(yh);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                result = x.Parameters.Length.CompareTo(y.Parameters.Length);
                if (result != 0)
                {
                    return result;
                }

                for (var i = 0; i < x.Parameters.Length; i++)
                {
                    var xh = SymbolEqualityComparer.Default.GetHashCode(x.Parameters[i].Type);
                    var yh = SymbolEqualityComparer.Default.GetHashCode(y.Parameters[i].Type);
                    result = xh.CompareTo(yh);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                return 0;
            }
        }
    }
}