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
        public static ClassDeclarationSyntax GenerateMetadata(Compilation compilation, MetadataModel metadataModel, LibraryTypes libraryTypes)
        {
            var configParam = "config".ToIdentifierName();
            var addSerializerMethod = configParam.Member("Serializers").Member("Add");
            var body = new List<StatementSyntax>();
            body.AddRange(
                metadataModel.SerializableTypes.Select(
                    type =>
                        (StatementSyntax)ExpressionStatement(
                            InvocationExpression(
                                addSerializerMethod,
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(TypeOfExpression(GetPartialSerializerTypeName(type)))))))
                ));
            var addProxyMethod = configParam.Member("InterfaceProxies").Member("Add");
            body.AddRange(
                metadataModel.GeneratedProxies.Select(
                    type =>
                        (StatementSyntax)ExpressionStatement(
                            InvocationExpression(
                                addProxyMethod,
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(TypeOfExpression(type.TypeSyntax))))))
                ));

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
        }

        public static TypeSyntax GetPartialSerializerTypeName(this ISerializableTypeDescription type)
        {
            var genericArity = type.TypeParameters.Length;
            var name = SerializerGenerator.GetSimpleClassName(type);
            if (genericArity > 0)
            {
                name += $"<{new string(',', genericArity - 1)}>";
            }

            return ParseTypeName(name);
        }

        public static TypeSyntax GetInvokableTypeName(this MethodDescription method)
        {
            var genericArity = method.Method.TypeParameters.Length + method.Method.ContainingType.TypeParameters.Length;
            var name = InvokableGenerator.GetSimpleClassName(method.Method);
            if (genericArity > 0)
            {
                name += $"<{new string(',', genericArity - 1)}>";
            }

            return ParseTypeName(name);
        }

        public static TypeSyntax GetProxyTypeName(this IGeneratedProxyDescription proxy)
        {
            var interfaceType = proxy.InterfaceDescription.InterfaceType;
            var genericArity = interfaceType.TypeParameters.Length;
            var name = ProxyGenerator.GetSimpleClassName(interfaceType);
            if (genericArity > 0)
            {
                name += $"<{new string(',', genericArity - 1)}>";
            }

            return ParseTypeName(name);
        }
    }
}