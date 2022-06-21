using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal interface IPropertyDescription : IMemberDescription 
    {
    }

    internal class PropertyDescription : IPropertyDescription
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

        public string AssemblyName => Type.ContainingAssembly.ToDisplayName();
        public string TypeName => Type.ToDisplayName();
        public string TypeNameIdentifier => Type.GetValidIdentifier();

        public TypeSyntax GetTypeSyntax(ITypeSymbol typeSymbol) => typeSymbol.ToTypeSyntax();
    }
}