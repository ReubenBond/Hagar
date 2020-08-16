using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class StaticCodecDescription : ICodecDescription
    {
        public StaticCodecDescription(ITypeSymbol underlyingType, INamedTypeSymbol codecType)
        {
            UnderlyingType = underlyingType;
            CodecType = codecType;
        }

        public ITypeSymbol UnderlyingType { get; }

        public INamedTypeSymbol CodecType { get; }
    }
}