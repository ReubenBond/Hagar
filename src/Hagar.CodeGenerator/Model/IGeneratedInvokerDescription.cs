using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class GeneratedInvokerDescription : ISerializableTypeDescription 
    {
        private readonly MethodDescription _methodDescription;
        private TypeSyntax _typeSyntax;

        public GeneratedInvokerDescription(
            InvokableInterfaceDescription interfaceDescription,
            MethodDescription methodDescription,
            Accessibility accessibility,
            string generatedClassName,
            List<IMemberDescription> members,
            List<INamedTypeSymbol> serializationHooks)
        {
            InterfaceDescription = interfaceDescription;
            _methodDescription = methodDescription;
            Name = generatedClassName;
            Members = members;

            Accessibility = accessibility;
            SerializationHooks = serializationHooks;
        }

        public Accessibility Accessibility { get; }
        public TypeSyntax TypeSyntax => _typeSyntax ??= CreateTypeSyntax();
        public bool HasComplexBaseType => false;
        public INamedTypeSymbol BaseType => throw new NotImplementedException();
        public string Namespace => GeneratedNamespace;
        public string GeneratedNamespace => InterfaceDescription.GeneratedNamespace;
        public string Name { get; }
        public bool IsValueType => false;
        public bool IsSealedType => true;
        public bool IsEnumType => false;
        public bool IsGenericType => TypeParameters.Count > 0;
        public List<IMemberDescription> Members { get; }
        public InvokableInterfaceDescription InterfaceDescription { get; }
        public SemanticModel SemanticModel => InterfaceDescription.SemanticModel;
        public bool IsEmptyConstructable => true;
        public bool IsPartial => true;
        public bool UseActivator => true; 
        public bool TrackReferences => false; 
        public bool OmitDefaultMemberValues => false;
        public List<(string Name, ITypeParameterSymbol Parameter)> TypeParameters => _methodDescription.AllTypeParameters;

        public List<INamedTypeSymbol> SerializationHooks { get; }

        public ExpressionSyntax GetObjectCreationExpression(LibraryTypes libraryTypes) => InvocationExpression(libraryTypes.InvokablePool.ToTypeSyntax().Member("Get", TypeSyntax))
            .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>()));

        private TypeSyntax CreateTypeSyntax()
        {
            var simpleName = InvokableGenerator.GetSimpleClassName(InterfaceDescription, _methodDescription);
            if (TypeParameters.Count > 0)
            {
                return QualifiedName(
                    ParseName(Namespace),
                    GenericName(
                        Identifier(simpleName),
                        TypeArgumentList(
                            SeparatedList<TypeSyntax>(TypeParameters.Select(p => IdentifierName(p.Name))))));
            }

            var name = QualifiedName(ParseName(Namespace), IdentifierName(simpleName));
            return name;
        }
    }

}