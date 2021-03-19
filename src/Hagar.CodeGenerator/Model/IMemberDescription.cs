using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hagar.CodeGenerator
{
    internal interface IMemberDescription
    {
        ushort FieldId { get; }
        ISymbol Member { get; }
        ITypeSymbol Type { get; }
        TypeSyntax TypeSyntax { get; }
        string Name { get; }
    }
}