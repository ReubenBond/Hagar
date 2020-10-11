using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal interface IMemberDescription
    {
        ushort FieldId { get; }
        ISymbol Member { get; }
        ITypeSymbol Type { get; }
        string Name { get; }
    }
}