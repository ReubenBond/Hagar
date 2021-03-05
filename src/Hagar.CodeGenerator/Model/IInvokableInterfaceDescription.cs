using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Hagar.CodeGenerator
{
    internal interface IInvokableInterfaceDescription
    {
        string Name { get; }
        INamedTypeSymbol InterfaceType { get; }
        List<MethodDescription> Methods { get; }
        INamedTypeSymbol ProxyBaseType { get; }
        bool IsExtension { get; }
        SemanticModel SemanticModel { get; }
    }
}