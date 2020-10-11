using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class PropertyDescription : IMemberDescription
    {
        public PropertyDescription(ushort fieldId, IPropertySymbol property)
        {
            FieldId = fieldId;
            Property = property;
        }

        public ushort FieldId { get; }
        public ISymbol Member => Property;
        public ITypeSymbol Type => Property.Type;
        public IPropertySymbol Property { get; }
        public string Name => Property.Name;
    }
}