using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal interface IMemberDescription
    {
        uint FieldId { get; }
        ISymbol Member { get; }
        ITypeSymbol Type { get; }
        string Name { get; }
    }
}