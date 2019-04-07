using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal interface IInvokableInterfaceDescription
    {
        INamedTypeSymbol InterfaceType { get; }
        List<MethodDescription> Methods { get; }
        INamedTypeSymbol ProxyBaseType { get; }
        bool IsExtension { get; }
        SemanticModel SemanticModel { get; }
    }
}