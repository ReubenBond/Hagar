using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class InvokableInterfaceDescription : IInvokableInterfaceDescription
    {
        public InvokableInterfaceDescription(
            LibraryTypes libraryTypes,
            SemanticModel semanticModel,
            INamedTypeSymbol interfaceType,
            IEnumerable<MethodDescription> methods,
            INamedTypeSymbol proxyBaseType,
            bool isExtension)
        {
            this.ValidateBaseClass(libraryTypes, proxyBaseType);
            this.SemanticModel = semanticModel;
            this.InterfaceType = interfaceType;
            this.ProxyBaseType = proxyBaseType;
            this.IsExtension = isExtension;
            this.Methods = methods.ToList();
        }

        void ValidateBaseClass(LibraryTypes l, INamedTypeSymbol baseClass)
        {
            var found = false;
            foreach (var member in baseClass.GetMembers("Invoke"))
            {
                if (!(member is IMethodSymbol method)) continue;
                if (method.TypeParameters.Length != 1) continue;
                if (method.Parameters.Length != 1) continue;
                if (!method.Parameters[0].Type.Equals(method.TypeParameters[0])) continue;
                if (!method.TypeParameters[0].ConstraintTypes.Contains(l.IInvokable)) continue;
                if (!method.ReturnType.Equals(l.ValueTask)) continue;
                found = true;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"Proxy base class {baseClass} does not contain a definition for ValueTask Invoke<T>(T) where T : IInvokable");
            }
        }

        public INamedTypeSymbol InterfaceType { get; }
        public List<MethodDescription> Methods { get; }
        public INamedTypeSymbol ProxyBaseType { get; }
        public bool IsExtension { get; }
        public SemanticModel SemanticModel { get; }
    }
}