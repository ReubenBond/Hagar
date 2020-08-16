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
        public SerializableTypeDescription(SemanticModel semanticModel, INamedTypeSymbol type, IEnumerable<IMemberDescription> members)
        {
            Type = type;
            Members = members.ToList();
            SemanticModel = semanticModel;
        }

        private INamedTypeSymbol Type { get; }

        public TypeSyntax TypeSyntax => Type.ToTypeSyntax();
        public TypeSyntax UnboundTypeSyntax => Type.ToTypeSyntax();

        public bool HasComplexBaseType => !IsValueType &&
                                          Type.BaseType != null &&
                                          Type.BaseType.SpecialType != SpecialType.System_Object;

        public INamedTypeSymbol BaseType => Type.BaseType;

        public string Name => Type.Name;

        public bool IsValueType => Type.IsValueType;

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

        public ExpressionSyntax GetObjectCreationExpression(LibraryTypes libraryTypes) => InvocationExpression(ObjectCreationExpression(TypeSyntax));
    }
}