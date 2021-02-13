using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class SerializableTypeDescription : ISerializableTypeDescription
    {
        private readonly LibraryTypes _libraryTypes;

        public SerializableTypeDescription(SemanticModel semanticModel, INamedTypeSymbol type, IEnumerable<IMemberDescription> members, LibraryTypes libraryTypes)
        {
            Type = type;
            Members = members.ToList();
            SemanticModel = semanticModel;
            _libraryTypes = libraryTypes;
        }

        private INamedTypeSymbol Type { get; }

        public TypeSyntax TypeSyntax => Type.ToTypeSyntax();
        public TypeSyntax UnboundTypeSyntax => Type.ToTypeSyntax();

        public bool HasComplexBaseType => !IsValueType &&
                                          Type.BaseType != null &&
                                          Type.BaseType.SpecialType != SpecialType.System_Object;

        public INamedTypeSymbol BaseType => Type.EnumUnderlyingType ?? Type.BaseType;

        public string Namespace => Type.ContainingNamespace.Name;

        public string Name => Type.Name;

        public bool IsValueType => Type.IsValueType;
        public bool IsSealedType => Type.IsSealed;
        public bool IsEnumType => Type.EnumUnderlyingType != null;

        public bool IsGenericType => Type.IsGenericType;

        public ImmutableArray<ITypeParameterSymbol> TypeParameters => Type.TypeParameters;

        public List<IMemberDescription> Members { get; }
        public SemanticModel SemanticModel { get; }

        public bool IsEmptyConstructable
        {
            get
            {
                if (Type.Constructors.Length == 0)
                {
                    return true;
                }

                foreach (var ctor in Type.Constructors)
                {
                    if (ctor.Parameters.Length != 0)
                    {
                        continue;
                    }

                    switch (ctor.DeclaredAccessibility)
                    {
                        case Accessibility.Public:
                            return true;
                    }
                }

                return false;
            }
        }

        public bool IsPartial
        {
            get
            {
                foreach (var reference in Type.DeclaringSyntaxReferences)
                {
                    var syntax = reference.GetSyntax();
                    if (syntax is TypeDeclarationSyntax typeDeclaration && typeDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool UseActivator => Type.HasAttribute(_libraryTypes.UseActivatorAttribute) || !IsEmptyConstructable;

        public ExpressionSyntax GetObjectCreationExpression(LibraryTypes libraryTypes) => InvocationExpression(ObjectCreationExpression(TypeSyntax));
    }
}