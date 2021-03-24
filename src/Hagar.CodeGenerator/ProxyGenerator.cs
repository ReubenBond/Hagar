using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    /// <summary>
    /// Generates RPC stub objects called invokers.
    /// </summary>
    internal static class ProxyGenerator
    {
        public static (ClassDeclarationSyntax, GeneratedProxyDescription) Generate(
            LibraryTypes libraryTypes,
            IInvokableInterfaceDescription interfaceDescription,
            MetadataModel metadataModel)
        {
            var generatedClassName = GetSimpleClassName(interfaceDescription);

            var ctors = GenerateConstructors(generatedClassName, interfaceDescription).ToArray();
            var proxyMethods = CreateProxyMethods(libraryTypes, interfaceDescription, metadataModel).ToArray();

            var classDeclaration = ClassDeclaration(generatedClassName)
                .AddBaseListTypes(
                    SimpleBaseType(interfaceDescription.ProxyBaseType.ToTypeSyntax()),
                    SimpleBaseType(interfaceDescription.InterfaceType.ToTypeSyntax()))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(
                    AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(ctors)
                .AddMembers(proxyMethods);

            var typeParameters = interfaceDescription.TypeParameters;
            if (typeParameters.Count > 0)
            {
                classDeclaration = SyntaxFactoryUtility.AddGenericTypeParameters(classDeclaration, typeParameters);
            }

            return (classDeclaration, new GeneratedProxyDescription(interfaceDescription));
        }

        public static string GetSimpleClassName(IInvokableInterfaceDescription interfaceDescription) => $"Proxy_{interfaceDescription.Name}";

        private static IEnumerable<MemberDeclarationSyntax> GenerateConstructors(
            string simpleClassName,
            IInvokableInterfaceDescription interfaceDescription)
        {
            var baseType = interfaceDescription.ProxyBaseType;
            foreach (var member in baseType.GetMembers())
            {
                if (member is not IMethodSymbol method)
                {
                    continue;
                }

                if (method.MethodKind != MethodKind.Constructor)
                {
                    continue;
                }

                if (method.DeclaredAccessibility == Accessibility.Private)
                {
                    continue;
                }

                yield return CreateConstructor(method);
            }

            ConstructorDeclarationSyntax CreateConstructor(IMethodSymbol baseConstructor)
            {
                return ConstructorDeclaration(simpleClassName)
                    .AddParameterListParameters(baseConstructor.Parameters.Select((p, i) => GetParameterSyntax(i, p, null)).ToArray())
                    .WithModifiers(TokenList(GetModifiers(baseConstructor)))
                    .WithInitializer(
                        ConstructorInitializer(
                            SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(
                                SeparatedList(baseConstructor.Parameters.Select(GetBaseInitializerArgument)))))
                    .WithBody(Block());
            }

            static IEnumerable<SyntaxToken> GetModifiers(IMethodSymbol method)
            {
                switch (method.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.Protected:
                        yield return Token(SyntaxKind.PublicKeyword);
                        break;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.ProtectedAndInternal:
                        yield return Token(SyntaxKind.InternalKeyword);
                        break;
                    default:
                        break;
                }
            }

            static ArgumentSyntax GetBaseInitializerArgument(IParameterSymbol parameter, int index)
            {
                var name = SyntaxFactoryUtility.GetSanitizedName(parameter, index);
                var result = Argument(IdentifierName(name));
                switch (parameter.RefKind)
                {
                    case RefKind.None:
                        break;
                    case RefKind.Ref:
                        result = result.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
                        break;
                    case RefKind.Out:
                        result = result.WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword));
                        break;
                    default:
                        break;
                }

                return result;
            }
        }

        private static IEnumerable<MemberDeclarationSyntax> CreateProxyMethods(
            LibraryTypes libraryTypes,
            IInvokableInterfaceDescription interfaceDescription,
            MetadataModel metadataModel)
        {
            foreach (var methodDescription in interfaceDescription.Methods)
            {
                yield return CreateProxyMethod(methodDescription);
            }

            MethodDeclarationSyntax CreateProxyMethod(MethodDescription methodDescription)
            {
                var method = methodDescription.Method;
                var declaration = MethodDeclaration(method.ReturnType.ToTypeSyntax(methodDescription.TypeParameterSubstitutions), method.Name.EscapeIdentifier())
                    .AddParameterListParameters(method.Parameters.Select((p, i) => GetParameterSyntax(i, p, methodDescription)).ToArray())
                    .WithBody(
                        CreateProxyMethodBody(libraryTypes, metadataModel, methodDescription));
                if (methodDescription.HasCollision)
                {
                    declaration = declaration.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));

                    // Type parameter constrains are not valid on explicit interface definitions
                    var typeParameters = SyntaxFactoryUtility.GetTypeParameterConstraints(methodDescription.MethodTypeParameters);
                    foreach (var (name, constraints) in typeParameters)
                    {
                        if (constraints.Count > 0)
                        {
                            declaration = declaration.AddConstraintClauses(
                                TypeParameterConstraintClause(name).AddConstraints(constraints.ToArray()));
                        }
                    }
                }
                else
                {
                    var explicitInterfaceSpecifier = ExplicitInterfaceSpecifier(methodDescription.Method.ContainingType.ToNameSyntax());
                    declaration = declaration.WithExplicitInterfaceSpecifier(explicitInterfaceSpecifier);
                }

                if (methodDescription.MethodTypeParameters.Count > 0)
                {
                    declaration = declaration.WithTypeParameterList(
                        TypeParameterList(SeparatedList(methodDescription.MethodTypeParameters.Select(tp => TypeParameter(tp.Name)))));
                }

                return declaration;
            }
        }

        private static BlockSyntax CreateProxyMethodBody(
            LibraryTypes libraryTypes,
            MetadataModel metadataModel,
            MethodDescription methodDescription)
        {
            var statements = new List<StatementSyntax>();

            var completionVar = IdentifierName("completion");
            var requestVar = IdentifierName("request");

            var requestDescription = metadataModel.GeneratedInvokables[methodDescription];
            var createRequestExpr = InvocationExpression(libraryTypes.InvokablePool.ToTypeSyntax().Member("Get", requestDescription.TypeSyntax))
                .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>()));

            statements.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        ParseTypeName("var"),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                    Identifier("request"))
                                .WithInitializer(
                                    EqualsValueClause(createRequestExpr))))));

            // Set request object fields from method parameters.
            var parameterIndex = 0;
            foreach (var parameter in methodDescription.Method.Parameters)
            {
                statements.Add(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            requestVar.Member($"arg{parameterIndex}"),
                            IdentifierName(SyntaxFactoryUtility.GetSanitizedName(parameter, parameterIndex)))));

                parameterIndex++;
            }

            ITypeSymbol returnType;
            var methodReturnType = (INamedTypeSymbol)methodDescription.Method.ReturnType;
            if (methodReturnType.TypeParameters.Length == 1)
            {
                returnType = methodReturnType.TypeArguments[0];
            }
            else
            {
                returnType = libraryTypes.Object;
            }

            var createCompletionExpr = InvocationExpression(libraryTypes.ResponseCompletionSourcePool.ToTypeSyntax().Member("Get", returnType.ToTypeSyntax(methodDescription.TypeParameterSubstitutions)))
                .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>()));
            statements.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        ParseTypeName("var"),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                    Identifier("completion"))
                                .WithInitializer(
                                    EqualsValueClause(createCompletionExpr))))));

            // Issue request
            statements.Add(
                ExpressionStatement(
                        InvocationExpression(
                            BaseExpression().Member("SendRequest"),
                            ArgumentList(SeparatedList(new[] { Argument(completionVar), Argument(requestVar) })))));

            // Return result
            string valueTaskMethodName;
            if (methodReturnType.TypeArguments.Length == 1)
            {
                valueTaskMethodName = "AsValueTask";
            }
            else if (SymbolEqualityComparer.Default.Equals(methodReturnType, libraryTypes.Void))
            {
                valueTaskMethodName = null;
            }
            else
            {
                valueTaskMethodName = "AsVoidValueTask";
            }

            if (valueTaskMethodName is not null)
            {
                var returnVal = InvocationExpression(completionVar.Member(valueTaskMethodName));

                if (SymbolEqualityComparer.Default.Equals(methodReturnType.ConstructedFrom, libraryTypes.Task_1) || SymbolEqualityComparer.Default.Equals(methodReturnType, libraryTypes.Task))
                {
                    returnVal = InvocationExpression(returnVal.Member("AsTask"));
                }

                statements.Add(ReturnStatement(returnVal));
            }

            return Block(statements);
        }

        private static ParameterSyntax GetParameterSyntax(int index, IParameterSymbol parameter, MethodDescription methodDescription)
        {
            var result = Parameter(Identifier(SyntaxFactoryUtility.GetSanitizedName(parameter, index))).WithType(parameter.Type.ToTypeSyntax(methodDescription?.TypeParameterSubstitutions));
            switch (parameter.RefKind)
            {
                case RefKind.None:
                    break;
                case RefKind.Ref:
                    result = result.WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));
                    break;
                case RefKind.Out:
                    result = result.WithModifiers(TokenList(Token(SyntaxKind.OutKeyword)));
                    break;
                case RefKind.In:
                    result = result.WithModifiers(TokenList(Token(SyntaxKind.InKeyword)));
                    break;
                default:
                    break;
            }

            return result;
        }
    }
}