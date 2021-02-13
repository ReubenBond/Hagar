using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class WellKnownCodecDescription : ICodecDescription
    {
        public WellKnownCodecDescription(ITypeSymbol underlyingType, INamedTypeSymbol codecType)
        {
            UnderlyingType = underlyingType;
            CodecType = codecType;
        }

        public ITypeSymbol UnderlyingType { get; }

        public INamedTypeSymbol CodecType { get; }
    }
}