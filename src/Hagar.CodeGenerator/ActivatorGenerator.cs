using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal static class ActivatorGenerator
    {
        public static ClassDeclarationSyntax GenerateActivator(LibraryTypes libraryTypes, ISerializableTypeDescription type)
        {
            var simpleClassName = GetSimpleClassName(type);

            var baseInterface = libraryTypes.IActivator_1.ToTypeSyntax(type.TypeSyntax);

            var classDeclaration = ClassDeclaration(simpleClassName)
                .AddBaseListTypes(SimpleBaseType(baseInterface))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(GenerateCreateMethod(libraryTypes, type));
            if (type.IsGenericType)
            {
                classDeclaration = AddGenericTypeConstraints(classDeclaration, type);
            }

            return classDeclaration;
        }

        public static string GetSimpleClassName(ISerializableTypeDescription serializableType)
        {
            var uniquifier = RuntimeHelpers.GetHashCode(serializableType).ToString("X");
            return $"{CodeGenerator.CodeGeneratorName}_Activator_{serializableType.Name}_{uniquifier}";
        }

        private static ClassDeclarationSyntax AddGenericTypeConstraints(ClassDeclarationSyntax classDeclaration, ISerializableTypeDescription type)
        {
            classDeclaration = classDeclaration.WithTypeParameterList(TypeParameterList(SeparatedList(type.TypeParameters.Select(tp => TypeParameter(tp.Name)))));
            var constraints = new List<TypeParameterConstraintSyntax>();
            foreach (var tp in type.TypeParameters)
            {
                constraints.Clear();
                if (tp.HasReferenceTypeConstraint)
                {
                    constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));
                }

                if (tp.HasValueTypeConstraint)
                {
                    constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));
                }

                foreach (var c in tp.ConstraintTypes)
                {
                    constraints.Add(TypeConstraint(c.ToTypeSyntax()));
                }

                if (tp.HasConstructorConstraint)
                {
                    constraints.Add(ConstructorConstraint());
                }

                if (constraints.Count > 0)
                {
                    classDeclaration = classDeclaration.AddConstraintClauses(TypeParameterConstraintClause(tp.Name).AddConstraints(constraints.ToArray()));
                }
            }

            return classDeclaration;
        }

        private static MemberDeclarationSyntax GenerateCreateMethod(LibraryTypes libraryTypes, ISerializableTypeDescription type)
        {
            var createObject = type.GetObjectCreationExpression(libraryTypes);

            return MethodDeclaration(type.TypeSyntax, "Create")
                .WithExpressionBody(ArrowExpressionClause(createObject))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .AddModifiers(Token(SyntaxKind.PublicKeyword));
        }
    }
}