using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    /// <summary>
    /// Generates RPC stub objects called invokers.
    /// </summary>
    internal static class InvokableGenerator
    {
        public static (ClassDeclarationSyntax, GeneratedInvokerDescription) Generate(
            LibraryTypes libraryTypes,
            InvokableInterfaceDescription interfaceDescription,
            MethodDescription method)
        {
            var generatedClassName = GetSimpleClassName(interfaceDescription, method);

            var fieldDescriptions = GetFieldDescriptions(method, interfaceDescription);
            var fields = GetFieldDeclarations(method, fieldDescriptions, libraryTypes);
            var ctor = GenerateConstructor(generatedClassName, method, fieldDescriptions);

            Accessibility accessibility = GetAccessibility(interfaceDescription);

            var targetField = fieldDescriptions.OfType<TargetFieldDescription>().Single();
            ITypeSymbol baseClassType = GetBaseClassType(method);

            var accessibilityKind = accessibility switch
            {
                Accessibility.Public => SyntaxKind.PublicKeyword,
                _ => SyntaxKind.InternalKeyword,
            };
            var classDeclaration = ClassDeclaration(generatedClassName)
                .AddBaseListTypes(SimpleBaseType(baseClassType.ToTypeSyntax(method.TypeParameterSubstitutions)))
                .AddModifiers(Token(accessibilityKind), Token(SyntaxKind.SealedKeyword), Token(SyntaxKind.PartialKeyword))
                .AddAttributeLists(
                    AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fields)
                .AddMembers(ctor)
                .AddMembers(
                    GenerateGetArgumentCount(libraryTypes, method),
                    GenerateSetTargetMethod(libraryTypes, interfaceDescription, targetField),
                    GenerateGetTargetMethod(targetField),
                    GenerateDisposeMethod(libraryTypes, method, fieldDescriptions),
                    GenerateGetArgumentMethod(libraryTypes, method, fieldDescriptions),
                    GenerateSetArgumentMethod(libraryTypes, method, fieldDescriptions),
                    GenerateInvokeInnerMethod(libraryTypes, method, fieldDescriptions, targetField));

            var typeParametersWithNames = method.AllTypeParameters;
            if (typeParametersWithNames.Count > 0)
            {
                classDeclaration = SyntaxFactoryUtility.AddGenericTypeParameters(classDeclaration, typeParametersWithNames);
            }

            List<INamedTypeSymbol> serializationHooks = new();
            if (baseClassType.GetAttributes(libraryTypes.SerializationCallbacksAttribute, out var hookAttributes))
            {
                foreach (var hookAttribute in hookAttributes)
                {
                    var hookType = (INamedTypeSymbol)hookAttribute.ConstructorArguments[0].Value;
                    serializationHooks.Add(hookType);
                }
            }

            var invokerDescription = new GeneratedInvokerDescription(
                interfaceDescription,
                method,
                accessibility,
                generatedClassName,
                fieldDescriptions.OfType<IMemberDescription>().ToList(),
                serializationHooks);
            return (classDeclaration, invokerDescription);

            static Accessibility GetAccessibility(InvokableInterfaceDescription interfaceDescription)
            {
                var t = interfaceDescription.InterfaceType;
                Accessibility accessibility = t.DeclaredAccessibility;
                while (t is not null)
                {
                    if ((int)t.DeclaredAccessibility < (int)accessibility)
                    {
                        accessibility = t.DeclaredAccessibility;
                    }

                    t = t.ContainingType;
                }

                return accessibility;
            }
        }

        private static ITypeSymbol GetBaseClassType(MethodDescription method)
        {
            var methodReturnType = (INamedTypeSymbol)method.Method.ReturnType;
            if (method.InvokableBaseTypes.TryGetValue(methodReturnType, out var baseClassType))
            {
                return baseClassType;
            }

            if (methodReturnType.ConstructedFrom is { } constructedFrom)
            {
                var unbound = constructedFrom.ConstructUnboundGenericType();
                if (method.InvokableBaseTypes.TryGetValue(unbound, out baseClassType))
                {
                    return baseClassType.ConstructedFrom.Construct(methodReturnType.TypeArguments.ToArray());
                }
            }
            
            throw new InvalidOperationException($"Unsupported return type {methodReturnType} for method {method.Method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
        }

        private static MemberDeclarationSyntax GenerateSetTargetMethod(
            LibraryTypes libraryTypes,
            InvokableInterfaceDescription interfaceDescription,
            TargetFieldDescription targetField)
        {
            var type = IdentifierName("TTargetHolder");
            var typeToken = type.Identifier;
            var holder = IdentifierName("holder");
            var holderParameter = holder.Identifier;

            var getTarget = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        holder,
                        GenericName(interfaceDescription.IsExtension ? "GetComponent" : "GetTarget")
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList(interfaceDescription.InterfaceType.ToTypeSyntax())))))
                .WithArgumentList(ArgumentList());

            var body =
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    ThisExpression().Member(targetField.FieldName),
                    getTarget);
            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "SetTarget")
                .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(TypeParameter(typeToken))))
                .WithParameterList(ParameterList(SingletonSeparatedList(Parameter(holderParameter).WithType(type))))
                .WithExpressionBody(ArrowExpressionClause(body))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateGetTargetMethod(
            TargetFieldDescription targetField)
        {
            var type = IdentifierName("TTarget");
            var typeToken = type.Identifier;

            var body = CastExpression(type, ThisExpression().Member(targetField.FieldName));
            return MethodDeclaration(type, "GetTarget")
                .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(TypeParameter(typeToken))))
                .WithParameterList(ParameterList())
                .WithExpressionBody(ArrowExpressionClause(body))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateGetArgumentMethod(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription,
            List<InvokerFieldDescripton> fields)
        {
            var index = IdentifierName("index");
            var type = IdentifierName("TArgument");
            var typeToken = type.Identifier;

            var cases = new List<SwitchSectionSyntax>();
            foreach (var field in fields)
            {
                if (field is not MethodParameterFieldDescription parameter)
                {
                    continue;
                }

                // C#: case {index}: return (TArgument)(object){field}
                var label = CaseSwitchLabel(
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(parameter.ParameterOrdinal)));
                cases.Add(
                    SwitchSection(
                        SingletonList<SwitchLabelSyntax>(label),
                        new SyntaxList<StatementSyntax>(
                            ReturnStatement(
                                CastExpression(
                                    type,
                                    CastExpression(
                                        libraryTypes.Object.ToTypeSyntax(),
                                        ThisExpression().Member(parameter.FieldName)))))));
            }

            // C#: default: return HagarGeneratedCodeHelper.InvokableThrowArgumentOutOfRange<TArgument>(index, {maxArgs})
            var throwHelperMethod = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("HagarGeneratedCodeHelper"),
                GenericName("InvokableThrowArgumentOutOfRange")
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList<TypeSyntax>(type))));
            cases.Add(
                SwitchSection(
                    SingletonList<SwitchLabelSyntax>(DefaultSwitchLabel()),
                    new SyntaxList<StatementSyntax>(
                        ReturnStatement(
                            InvocationExpression(
                                throwHelperMethod,
                                ArgumentList(
                                    SeparatedList(
                                        new[]
                                        {
                                            Argument(index),
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(
                                                        Math.Max(0, methodDescription.Method.Parameters.Length - 1))))
                                        })))))));
            var body = SwitchStatement(ParenthesizedExpression(index), new SyntaxList<SwitchSectionSyntax>(cases));
            return MethodDeclaration(type, "GetArgument")
                .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(TypeParameter(typeToken))))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("index")).WithType(libraryTypes.Int32.ToTypeSyntax()))))
                .WithBody(Block(body))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateSetArgumentMethod(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription,
            List<InvokerFieldDescripton> fields)
        {
            var index = IdentifierName("index");
            var value = IdentifierName("value");
            var type = IdentifierName("TArgument");
            var typeToken = type.Identifier;

            var cases = new List<SwitchSectionSyntax>();
            foreach (var field in fields)
            {
                if (field is not MethodParameterFieldDescription parameter)
                {
                    continue;
                }

                // C#: case {index}: {field} = (TField)(object)value; return;
                var label = CaseSwitchLabel(
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(parameter.ParameterOrdinal)));
                cases.Add(
                    SwitchSection(
                        SingletonList<SwitchLabelSyntax>(label),
                        new SyntaxList<StatementSyntax>(
                            new StatementSyntax[]
                            {
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        ThisExpression().Member(parameter.FieldName),
                                        CastExpression(
                                            parameter.FieldType.ToTypeSyntax(methodDescription.TypeParameterSubstitutions),
                                            CastExpression(
                                                libraryTypes.Object.ToTypeSyntax(),
                                                value
                                            )))),
                                ReturnStatement()
                            })));
            }

            // C#: default: return HagarGeneratedCodeHelper.InvokableThrowArgumentOutOfRange<TArgument>(index, {maxArgs})
            var maxArgs = Math.Max(0, methodDescription.Method.Parameters.Length - 1);
            var throwHelperMethod = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("HagarGeneratedCodeHelper"),
                GenericName("InvokableThrowArgumentOutOfRange")
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList<TypeSyntax>(type))));
            cases.Add(
                SwitchSection(
                    SingletonList<SwitchLabelSyntax>(DefaultSwitchLabel()),
                    new SyntaxList<StatementSyntax>(
                        new StatementSyntax[]
                        {
                            ExpressionStatement(
                                InvocationExpression(
                                    throwHelperMethod,
                                    ArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                                Argument(index),
                                                Argument(
                                                    LiteralExpression(
                                                        SyntaxKind.NumericLiteralExpression,
                                                        Literal(maxArgs)))
                                            })))),
                            ReturnStatement()
                        })));
            var body = SwitchStatement(ParenthesizedExpression(index), new SyntaxList<SwitchSectionSyntax>(cases));
            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "SetArgument")
                .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(TypeParameter(typeToken))))
                .WithParameterList(
                    ParameterList(
                        SeparatedList(
                            new[]
                            {
                                Parameter(Identifier("index")).WithType(libraryTypes.Int32.ToTypeSyntax()),
                                Parameter(Identifier("value"))
                                    .WithType(type)
                                    .WithModifiers(TokenList(Token(SyntaxKind.InKeyword)))
                            }
                        )))
                .WithBody(Block(body))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateInvokeInnerMethod(
            LibraryTypes libraryTypes,
            MethodDescription method,
            List<InvokerFieldDescripton> fields,
            TargetFieldDescription target)
        {
            var resultTask = IdentifierName("resultTask");

            // C# var resultTask = this.target.{Method}({params});
            var args = SeparatedList(
                fields.OfType<MethodParameterFieldDescription>()
                    .OrderBy(p => p.ParameterOrdinal)
                    .Select(p => Argument(ThisExpression().Member(p.FieldName))));
            ExpressionSyntax methodCall;
            if (method.MethodTypeParameters.Count > 0)
            {
                methodCall = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression().Member(target.FieldName),
                    GenericName(
                        Identifier(method.Method.Name),
                        TypeArgumentList(
                            SeparatedList<TypeSyntax>(
                                method.MethodTypeParameters.Select(p => IdentifierName(p.Name))))));
            }
            else
            {
                methodCall = ThisExpression().Member(target.FieldName).Member(method.Method.Name);
            }

            return MethodDeclaration(method.Method.ReturnType.ToTypeSyntax(method.TypeParameterSubstitutions), "InvokeInner")
                .WithParameterList(ParameterList())
                .WithExpressionBody(ArrowExpressionClause(InvocationExpression(methodCall, ArgumentList(args))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateDisposeMethod(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription,
            List<InvokerFieldDescripton> fields)
        {
            var body = new List<StatementSyntax>();

            foreach (var field in fields)
            {
                if (!field.IsInjected)
                {
                    body.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                ThisExpression().Member(field.FieldName),
                                DefaultExpression(field.FieldType.ToTypeSyntax(methodDescription.TypeParameterSubstitutions)))));
                }
            }

            body.Add(ExpressionStatement(InvocationExpression(libraryTypes.InvokablePool.ToTypeSyntax().Member("Return"))
                .WithArgumentList(ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(ThisExpression()))))));

            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "Dispose")
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithBody(Block(body));
        }

        private static MemberDeclarationSyntax GenerateGetArgumentCount(
            LibraryTypes libraryTypes,
            MethodDescription methodDescription) =>
            PropertyDeclaration(libraryTypes.Int32.ToTypeSyntax(), "ArgumentCount")
                .WithExpressionBody(
                    ArrowExpressionClause(
                        LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(methodDescription.Method.Parameters.Length))))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        public static string GetSimpleClassName(InvokableInterfaceDescription interfaceDescription, MethodDescription method)
        {
            var genericArity = method.AllTypeParameters.Count;
            var typeArgs = genericArity > 0 ? "_" + genericArity : string.Empty;
            return $"Invokable_{interfaceDescription.Name}_{interfaceDescription.ProxyBaseType.Name}_{method.Name}{typeArgs}";
        }

        private static MemberDeclarationSyntax[] GetFieldDeclarations(
            MethodDescription method,
            List<InvokerFieldDescripton> fieldDescriptions,
            LibraryTypes libraryTypes)
        {
            return fieldDescriptions.Select(GetFieldDeclaration).ToArray();

            MemberDeclarationSyntax GetFieldDeclaration(InvokerFieldDescripton description)
            {
                var field = FieldDeclaration(
                    VariableDeclaration(
                        description.FieldType.ToTypeSyntax(method.TypeParameterSubstitutions),
                        SingletonSeparatedList(VariableDeclarator(description.FieldName))));

                switch (description)
                {
                    case MethodParameterFieldDescription _:
                        field = field.AddModifiers(Token(SyntaxKind.PublicKeyword));
                        break;
                }

                if (!description.IsSerializable)
                {
                    field = field.AddAttributeLists(
                            AttributeList()
                                .AddAttributes(Attribute(libraryTypes.NonSerializedAttribute.ToNameSyntax())));
                }
                else if (description is MethodParameterFieldDescription parameter)
                {
                    field = field.AddAttributeLists(
                        AttributeList()
                            .AddAttributes(
                                Attribute(
                                    libraryTypes.IdAttributeTypes.First().ToNameSyntax(),
                                    AttributeArgumentList()
                                        .AddArguments(
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    Literal(parameter.FieldId)))))));
                }

                return field;
            }
        }

        private static ConstructorDeclarationSyntax GenerateConstructor(
            string simpleClassName,
            MethodDescription method,
            List<InvokerFieldDescripton> fieldDescriptions)
        {
            var injected = fieldDescriptions.Where(f => f.IsInjected).ToList();

            var parameters = injected.Select(
                f => Parameter(f.FieldName.ToIdentifier()).WithType(f.FieldType.ToTypeSyntax(method.TypeParameterSubstitutions)));

            var body = injected.Select(
                f => (StatementSyntax)ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        ThisExpression().Member(f.FieldName.ToIdentifierName()),
                        Unwrapped(f.FieldName.ToIdentifierName())))).ToList();

            foreach (var (methodName, methodArgument) in method.CustomInitializerMethods)
            {
                var argumentExpression = methodArgument.ToExpression();
                body.Add(ExpressionStatement(InvocationExpression(ThisExpression().Member(methodName), ArgumentList(SeparatedList(new[] { Argument(argumentExpression) })))));
            }

            return ConstructorDeclaration(simpleClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters.ToArray())
                .AddBodyStatements(body.ToArray());

            static ExpressionSyntax Unwrapped(ExpressionSyntax expr)
            {
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("HagarGeneratedCodeHelper"),
                        IdentifierName("UnwrapService")),
                    ArgumentList(SeparatedList(new[] { Argument(ThisExpression()), Argument(expr) })));
            }
        }

        private static List<InvokerFieldDescripton> GetFieldDescriptions(
            MethodDescription method,
            InvokableInterfaceDescription interfaceDescription)
        {
            var fields = new List<InvokerFieldDescripton>();

            ushort fieldId = 0;
            foreach (var parameter in method.Method.Parameters)
            {
                fields.Add(new MethodParameterFieldDescription(method, parameter, $"arg{fieldId}", fieldId));
                fieldId++;
            }

            fields.Add(new TargetFieldDescription(method, interfaceDescription.InterfaceType));

            return fields;
        }

        internal abstract class InvokerFieldDescripton
        {
            protected InvokerFieldDescripton(ITypeSymbol fieldType, string fieldName)
            {
                FieldType = fieldType;
                FieldName = fieldName;
            }

            public ITypeSymbol FieldType { get; }
            public abstract TypeSyntax FieldTypeSyntax { get; }
            public string FieldName { get; }
            public abstract bool IsInjected { get; }
            public abstract bool IsSerializable { get; }
        }

        internal class TargetFieldDescription : InvokerFieldDescripton
        {
            private readonly MethodDescription _method;

            public TargetFieldDescription(MethodDescription method, ITypeSymbol fieldType) : base(fieldType, "target")
            {
                _method = method;
            }

            public override bool IsInjected => false;
            public override bool IsSerializable => false;
            public override TypeSyntax FieldTypeSyntax => FieldType.ToTypeSyntax(_method.TypeParameterSubstitutions);
        }

        internal class MethodParameterFieldDescription : InvokerFieldDescripton, IMemberDescription
        {
            private readonly MethodDescription _method;

            public MethodParameterFieldDescription(MethodDescription method, IParameterSymbol parameter, string fieldName, ushort fieldId)
                : base(parameter.Type, fieldName)
            {
                _method = method;
                FieldId = fieldId;
                Parameter = parameter;
                if (parameter.Type.TypeKind == TypeKind.Dynamic)
                {
                    TypeSyntax = PredefinedType(Token(SyntaxKind.ObjectKeyword));
                    TypeName = "dynamic";
                }
                else
                {
                    TypeName = Type.ToDisplayName(method.TypeParameterSubstitutions);
                    TypeSyntax = Type.ToTypeSyntax(method.TypeParameterSubstitutions);
                }

                FieldTypeSyntax = TypeSyntax;
            }

            public int ParameterOrdinal => Parameter.Ordinal;
            public override bool IsInjected => false;

            public ushort FieldId { get; }
            public ISymbol Member => Parameter;
            public ITypeSymbol Type => FieldType;
            public TypeSyntax TypeSyntax { get; }
            public IParameterSymbol Parameter { get; }
            public string Name => FieldName;
            public override bool IsSerializable => true;
            public override TypeSyntax FieldTypeSyntax { get; }

            public string AssemblyName => Parameter.Type.ContainingAssembly.ToDisplayName();
            public string TypeName { get; }

            public string TypeNameIdentifier
            {
                get
                {
                    if (Type is ITypeParameterSymbol tp && _method.TypeParameterSubstitutions.TryGetValue(tp, out var name))
                    {
                        return name;
                    }

                    return Type.GetValidIdentifier();
                }
            }

            public TypeSyntax GetTypeSyntax(ITypeSymbol typeSymbol) => typeSymbol.ToTypeSyntax(_method.TypeParameterSubstitutions);
        }
    }
}