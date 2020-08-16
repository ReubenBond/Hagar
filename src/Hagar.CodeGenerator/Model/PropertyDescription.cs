using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class PropertyDescription : IMemberDescription
    {
        public PropertyDescription(uint fieldId, IPropertySymbol property)
        {
            FieldId = fieldId;
            Property = property;
        }

        public uint FieldId { get; }
        public ISymbol Member => Property;
        public ITypeSymbol Type => Property.Type;
        public IPropertySymbol Property { get; }
        public string Name => Property.Name;
    }
}