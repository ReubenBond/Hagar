using System;
using System.Collections.Generic;
using System.Linq;
using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal static class MetadataGenerator
    {
        public static ClassDeclarationSyntax GenerateMetadata(Compilation compilation, List<TypeDescription> serializableTypes)
        {
            var configParam = "config".ToIdentifierName();
            var addMethod = configParam.Member("PartialSerializers").Member("Add");
            var body = new List<StatementSyntax>();
            body.AddRange(
                serializableTypes.Select(
                    type =>
                        (StatementSyntax) ExpressionStatement(InvocationExpression(addMethod, ArgumentList(SingletonSeparatedList(Argument(TypeOfExpression(GetPartialSerializerTypeName(type.Type)))))))
                ));

            var libraryTypes = LibraryTypes.FromCompilation(compilation);
            var configType = libraryTypes.SerializerConfiguration;
            var configureMethod = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), "Configure")
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(configParam.Identifier).WithType(configType.ToTypeSyntax()))
                .AddBodyStatements(body.ToArray());

            var interfaceType = libraryTypes.ConfigurationProvider.Construct(configType);
            return ClassDeclaration(CodeGenerator.CodeGeneratorName + "_Metadata_" + compilation.AssemblyName.Replace('.', '_'))
                .AddBaseListTypes(SimpleBaseType(interfaceType.ToTypeSyntax()))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(configureMethod);

            TypeSyntax GetPartialSerializerTypeName(INamedTypeSymbol type)
            {
                var genericArity = type.TypeParameters.Length;
                var name = PartialSerializerGenerator.GetSimpleClassName(type);
                if (genericArity > 0)
                {
                    name += $"<{new string(',', genericArity - 1)}>";
                }

                return ParseTypeName(name);
            }
        }
    }
}