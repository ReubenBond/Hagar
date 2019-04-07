using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hagar.CodeGenerator
{
    internal interface ISerializableTypeDescription
    {
        TypeSyntax TypeSyntax { get; }
        TypeSyntax UnboundTypeSyntax { get; }
        bool HasComplexBaseType { get; }
        INamedTypeSymbol BaseType { get; }
        string Name { get; }
        bool IsValueType { get; }
        bool IsGenericType { get; }
        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }
        List<IMemberDescription> Members { get; }
        SemanticModel SemanticModel { get; }
    }
}