using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class FieldDescription : IFieldDescription
    {
        public FieldDescription(uint fieldId, IFieldSymbol field)
        {
            FieldId = fieldId;
            Field = field;
        }
        public IFieldSymbol Field { get; }
        public uint FieldId { get; }
        public ISymbol Member => Field;
        public ITypeSymbol Type => Field.Type;
        public string Name => Field.Name;
    }

    internal interface IFieldDescription : IMemberDescription
    {
    }

}