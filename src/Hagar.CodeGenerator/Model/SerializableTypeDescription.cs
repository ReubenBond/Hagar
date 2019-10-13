using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class SerializableTypeDescription : ISerializableTypeDescription
    {
        public SerializableTypeDescription(SemanticModel semanticModel, INamedTypeSymbol type, IEnumerable<IMemberDescription> members)
        {
            this.Type = type;
            this.Members = members.ToList();
            this.SemanticModel = semanticModel;
        }

        private INamedTypeSymbol Type { get; }

        public TypeSyntax TypeSyntax => this.Type.ToTypeSyntax();
        public TypeSyntax UnboundTypeSyntax => this.Type.ToTypeSyntax();

        public bool HasComplexBaseType => !this.IsValueType &&
                                          this.Type.BaseType != null &&
                                          this.Type.BaseType.SpecialType != SpecialType.System_Object;

        public INamedTypeSymbol BaseType => this.Type.BaseType;

        public string Name => this.Type.Name;

        public bool IsValueType => this.Type.IsValueType;

        public bool IsGenericType => this.Type.IsGenericType;

        public ImmutableArray<ITypeParameterSymbol> TypeParameters => this.Type.TypeParameters;

        public List<IMemberDescription> Members { get; }
        public SemanticModel SemanticModel { get; }

        public bool IsEmptyConstructable
        {
            get
            {
                if (this.Type.Constructors.Length == 0) return true;
                foreach (var ctor in this.Type.Constructors)
                {
                    if (ctor.Parameters.Length != 0) continue;
                    switch (ctor.DeclaredAccessibility)
                    {
                        case Accessibility.Public:
                            return true;
                    }
                }

                return false;
            }
        }

        public ExpressionSyntax GetObjectCreationExpression(LibraryTypes libraryTypes) => InvocationExpression(ObjectCreationExpression(this.TypeSyntax));
    }
}