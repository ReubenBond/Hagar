using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                classDeclaration = SyntaxFactoryUtility.AddGenericTypeParameters(classDeclaration, type.TypeParameters);
            }

            return classDeclaration;
        }

        public static string GetSimpleClassName(ISerializableTypeDescription serializableType) => $"Activator_{serializableType.Name}";

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