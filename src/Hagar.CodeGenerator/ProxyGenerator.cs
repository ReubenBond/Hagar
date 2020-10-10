using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    /// <summary>
    /// Generates RPC stub objects called invokers.
    /// </summary>
    internal static class ProxyGenerator
    {
        public static (ClassDeclarationSyntax, IGeneratedProxyDescription) Generate(
            LibraryTypes libraryTypes,
            IInvokableInterfaceDescription interfaceDescription,
            MetadataModel metadataModel)
        {
            var generatedClassName = GetSimpleClassName(interfaceDescription.InterfaceType);

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

            if (interfaceDescription.InterfaceType.TypeParameters.Length > 0)
            {
                classDeclaration = AddGenericTypeConstraints(classDeclaration, interfaceDescription.InterfaceType);
            }

            return (classDeclaration, new GeneratedProxyDescription(interfaceDescription));
        }

        private class GeneratedProxyDescription : IGeneratedProxyDescription
        {
            public GeneratedProxyDescription(IInvokableInterfaceDescription interfaceDescription)
            {
                InterfaceDescription = interfaceDescription;
            }

            public TypeSyntax TypeSyntax => this.GetProxyTypeName();
            public IInvokableInterfaceDescription InterfaceDescription { get; }
        }

        public static string GetSimpleClassName(INamedTypeSymbol type) => $"{CodeGenerator.CodeGeneratorName}_Proxy_{type.Name}";

        private static ClassDeclarationSyntax AddGenericTypeConstraints(
            ClassDeclarationSyntax classDeclaration,
            INamedTypeSymbol type)
        {
            var typeParameters = GetTypeParametersWithConstraints(type.TypeParameters);
            foreach (var (name, constraints) in typeParameters)
            {
                if (constraints.Count > 0)
                {
                    classDeclaration = classDeclaration.AddConstraintClauses(
                        TypeParameterConstraintClause(name).AddConstraints(constraints.ToArray()));
                }
            }

            if (typeParameters.Count > 0)
            {
                classDeclaration = classDeclaration.WithTypeParameterList(
                    TypeParameterList(SeparatedList(typeParameters.Select(tp => TypeParameter(tp.Item1)))));
            }

            return classDeclaration;
        }

        private static List<(string, List<TypeParameterConstraintSyntax>)> GetTypeParametersWithConstraints(ImmutableArray<ITypeParameterSymbol> typeParameter)
        {
            var allConstraints = new List<(string, List<TypeParameterConstraintSyntax>)>();
            foreach (var tp in typeParameter)
            {
                var constraints = new List<TypeParameterConstraintSyntax>();
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

                allConstraints.Add((tp.Name, constraints));
            }

            return allConstraints;
        }

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
                    .AddParameterListParameters(baseConstructor.Parameters.Select(GetParameterSyntax).ToArray())
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

            static ArgumentSyntax GetBaseInitializerArgument(IParameterSymbol parameter)
            {
                var result = Argument(IdentifierName(parameter.Name));
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
                var declaration = MethodDeclaration(method.ReturnType.ToTypeSyntax(), method.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(method.Parameters.Select(GetParameterSyntax).ToArray())
                    .WithBody(
                        CreateProxyMethodBody(libraryTypes, metadataModel, methodDescription));

                var typeParameters = GetTypeParametersWithConstraints(method.TypeParameters);
                foreach (var (name, constraints) in typeParameters)
                {
                    if (constraints.Count > 0)
                    {
                        declaration = declaration.AddConstraintClauses(
                            TypeParameterConstraintClause(name).AddConstraints(constraints.ToArray()));
                    }
                }

                if (typeParameters.Count > 0)
                {
                    declaration = declaration.WithTypeParameterList(
                        TypeParameterList(SeparatedList(typeParameters.Select(tp => TypeParameter(tp.Item1)))));
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
                            IdentifierName(parameter.Name))));

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

            var createCompletionExpr = InvocationExpression(libraryTypes.ResponseCompletionSourcePool.ToTypeSyntax().Member("Get", returnType.ToTypeSyntax()))
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
            else
            {
                valueTaskMethodName = "AsVoidValueTask";
            }

            var returnVal = InvocationExpression(completionVar.Member(valueTaskMethodName));

            if (SymbolEqualityComparer.Default.Equals(methodReturnType.ConstructedFrom, libraryTypes.Task_1) || SymbolEqualityComparer.Default.Equals(methodReturnType, libraryTypes.Task))
            {
                returnVal = InvocationExpression(returnVal.Member("AsTask"));
            }

            statements.Add(ReturnStatement(returnVal));

            return Block(statements);
        }

        private static ParameterSyntax GetParameterSyntax(IParameterSymbol parameter)
        {
            var result = Parameter(Identifier(parameter.Name)).WithType(parameter.Type.ToTypeSyntax());
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

        internal abstract class FieldDescription
        {
            protected FieldDescription(ITypeSymbol fieldType, string fieldName)
            {
                FieldType = fieldType;
                FieldName = fieldName;
            }

            public ITypeSymbol FieldType { get; }
            public string FieldName { get; }
            public abstract bool IsInjected { get; }
        }

        internal class InjectedFieldDescription : FieldDescription
        {
            public InjectedFieldDescription(ITypeSymbol fieldType, string fieldName) : base(fieldType, fieldName)
            {
            }

            public override bool IsInjected => true;
        }

        internal class CodecFieldDescription : FieldDescription, ICodecDescription
        {
            public CodecFieldDescription(ITypeSymbol fieldType, string fieldName, ITypeSymbol underlyingType)
                : base(fieldType, fieldName)
            {
                UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => true;
        }

        internal class TypeFieldDescription : FieldDescription
        {
            public TypeFieldDescription(ITypeSymbol fieldType, string fieldName, ITypeSymbol underlyingType) : base(
                fieldType,
                fieldName)
            {
                UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => false;
        }

        internal class MethodParameterFieldDescription : FieldDescription, IMemberDescription
        {
            public MethodParameterFieldDescription(IParameterSymbol parameter, string fieldName, uint fieldId)
                : base(parameter.Type, fieldName)
            {
                FieldId = fieldId;
                Parameter = parameter;
            }

            public override bool IsInjected => false;
            public uint FieldId { get; }
            public ISymbol Member => Parameter;
            public ITypeSymbol Type => FieldType;
            public IParameterSymbol Parameter { get; }
            public string Name => FieldName;
        }
    }
}