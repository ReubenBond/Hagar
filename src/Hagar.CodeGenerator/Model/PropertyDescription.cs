using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class PropertyDescription : IMemberDescription
    {
        public PropertyDescription(ushort fieldId, IPropertySymbol property)
        {
            FieldId = fieldId;
            Property = property;
            if (Type.TypeKind == TypeKind.Dynamic)
            {
                TypeSyntax = PredefinedType(Token(SyntaxKind.ObjectKeyword));
            }
            else
            {
                TypeSyntax = Type.ToTypeSyntax();
            }
        }

        public ushort FieldId { get; }
        public ISymbol Member => Property;
        public ITypeSymbol Type => Property.Type;
        public IPropertySymbol Property { get; }
        public string Name => Property.Name;

        public TypeSyntax TypeSyntax { get; }
    }
}