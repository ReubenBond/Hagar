using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

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
        bool IsEnumType { get; }
        bool IsGenericType { get; }
        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }
        List<IMemberDescription> Members { get; }
        SemanticModel SemanticModel { get; }
        bool IsEmptyConstructable { get; }
        ExpressionSyntax GetObjectCreationExpression(LibraryTypes libraryTypes);
    }
}