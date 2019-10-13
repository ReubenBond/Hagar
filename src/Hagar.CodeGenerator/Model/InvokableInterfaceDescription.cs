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
            foreach (var member in baseClass.GetMembers("SendRequest"))
            {
                if (!(member is IMethodSymbol method)) Throw(member, "not method");
                if (method.TypeParameters.Length != 0) Throw(member, "type params");
                if (method.Parameters.Length != 2) Throw(member, "params length");
                if (!method.Parameters[0].Type.Equals(l.IResponseCompletionSource)) Throw(member, "param 0");
                if (!method.Parameters[1].Type.Equals(l.IInvokable)) Throw(member, "param 1");
                if (!method.ReturnsVoid) Throw(member, "return type");
                found = true;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"Proxy base class {baseClass} does not contain a definition for void SendRequest(IResponseCompletionSource, IInvokable)");
            }

            void Throw(ISymbol m, string x) => throw new InvalidOperationException("Complaint: " + x + " for symbol: " + m.ToDisplayString());
        }

        public INamedTypeSymbol InterfaceType { get; }
        public List<MethodDescription> Methods { get; }
        public INamedTypeSymbol ProxyBaseType { get; }
        public bool IsExtension { get; }
        public SemanticModel SemanticModel { get; }
    }
}