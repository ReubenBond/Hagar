using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal interface ICodecDescription
    {
        ITypeSymbol UnderlyingType { get; }
    }

    internal interface ICopierDescription
    {
        ITypeSymbol UnderlyingType { get; }
    }
}