using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Hagar.CodeGenerator
{
    internal interface IInvokableInterfaceDescription
    {
        string Name { get; }
        string GeneratedNamespace { get; }
        INamedTypeSymbol InterfaceType { get; }
        List<MethodDescription> Methods { get; }
        List<(string Name, ITypeParameterSymbol Parameter)> TypeParameters { get; }
        INamedTypeSymbol ProxyBaseType { get; }
        bool IsExtension { get; }
        SemanticModel SemanticModel { get; }
    }
}