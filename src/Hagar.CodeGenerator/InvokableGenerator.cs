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
            IInvokableInterfaceDescription interfaceDescription,
            MethodDescription methodDescription)
        {
            var method = methodDescription.Method;
            var generatedClassName = GetSimpleClassName(interfaceDescription, methodDescription);

            var fieldDescriptions = GetFieldDescriptions(methodDescription, interfaceDescription);
            var fields = GetFieldDeclarations(fieldDescriptions, libraryTypes);
            var ctor = GenerateConstructor(generatedClassName, fieldDescriptions);

            Accessibility accessibility = GetAccessibility(interfaceDescription);
            var invokerDescription = new GeneratedInvokerDescription(
                                interfaceDescription,
                                methodDescription,
                                accessibility,
                                generatedClassName,
                                fieldDescriptions.OfType<IMemberDescription>().ToList());

            var targetField = fieldDescriptions.OfType<TargetFieldDescription>().Single();
            var methodReturnType = (INamedTypeSymbol)method.ReturnType;

            ITypeSymbol baseClassType = GetBaseClassType(libraryTypes, methodReturnType);

            var accessibilityKind = accessibility switch
            {
                Accessibility.Public => SyntaxKind.PublicKeyword,
                _ => SyntaxKind.InternalKeyword,
            };
            var classDeclaration = ClassDeclaration(generatedClassName)
                .AddBaseListTypes(SimpleBaseType(baseClassType.ToTypeSyntax()))
                .AddModifiers(Token(accessibilityKind), Token(SyntaxKind.SealedKeyword), Token(SyntaxKind.PartialKeyword))
                .AddAttributeLists(
                    AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fields)
                .AddMembers(ctor)
                .AddMembers(
                    GenerateGetArgumentCount(libraryTypes, methodDescription),
                    GenerateSetTargetMethod(libraryTypes, interfaceDescription, targetField),
                    GenerateGetTargetMethod(targetField),
                    GenerateDisposeMethod(libraryTypes, fieldDescriptions),
                    GenerateGetArgumentMethod(libraryTypes, methodDescription, fieldDescriptions),
                    GenerateSetArgumentMethod(libraryTypes, methodDescription, fieldDescriptions),
                    GenerateInvokeInnerMethod(libraryTypes, methodDescription, fieldDescriptions, targetField));

            var typeParametersWithNames = methodDescription.AllTypeParameters;
            if (typeParametersWithNames.Count > 0)
            {
                classDeclaration = SyntaxFactoryUtility.AddGenericTypeParameters(classDeclaration, typeParametersWithNames);
            }

            return (classDeclaration, invokerDescription);

            static Accessibility GetAccessibility(IInvokableInterfaceDescription interfaceDescription)
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

        private static ITypeSymbol GetBaseClassType(LibraryTypes libraryTypes, INamedTypeSymbol methodReturnType)
        {
            ITypeSymbol baseClassType;
            if (methodReturnType.TypeArguments.Length == 1)
            {
                if (SymbolEqualityComparer.Default.Equals(methodReturnType.ConstructedFrom, libraryTypes.ValueTask_1))
                {
                    baseClassType = libraryTypes.Request_1.Construct(methodReturnType.TypeArguments[0]);
                }
                else if (SymbolEqualityComparer.Default.Equals(methodReturnType.ConstructedFrom, libraryTypes.Task_1))
                {
                    baseClassType = libraryTypes.TaskRequest_1.Construct(methodReturnType.TypeArguments[0]);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported return type {methodReturnType}. Constructed from: {methodReturnType.ConstructedFrom}");
                }
            }
            else
            {
                if (SymbolEqualityComparer.Default.Equals(methodReturnType, libraryTypes.ValueTask))
                {
                    baseClassType = libraryTypes.Request;
                }
                else if (SymbolEqualityComparer.Default.Equals(methodReturnType, libraryTypes.Task))
                {
                    baseClassType = libraryTypes.TaskRequest;
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported return type {methodReturnType}");
                }
            }

            return baseClassType;
        }

        private static MemberDeclarationSyntax GenerateSetTargetMethod(
            LibraryTypes libraryTypes,
            IInvokableInterfaceDescription interfaceDescription,
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
                                            parameter.FieldType.ToTypeSyntax(),
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

            return MethodDeclaration(method.Method.ReturnType.ToTypeSyntax(), "InvokeInner")
                .WithParameterList(ParameterList())
                .WithExpressionBody(ArrowExpressionClause(InvocationExpression(methodCall, ArgumentList(args))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithModifiers(TokenList(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateDisposeMethod(
            LibraryTypes libraryTypes,
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
                                DefaultExpression(field.FieldType.ToTypeSyntax()))));
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

        public static string GetSimpleClassName(IInvokableInterfaceDescription interfaceDescription, MethodDescription method)
        {
            var genericArity = method.AllTypeParameters.Count;
            var typeArgs = genericArity > 0 ? "_" + genericArity : string.Empty;
            return $"Invokable_{interfaceDescription.Name}_{method.Name}{typeArgs}";
        }

        private static MemberDeclarationSyntax[] GetFieldDeclarations(
            List<InvokerFieldDescripton> fieldDescriptions,
            LibraryTypes libraryTypes)
        {
            return fieldDescriptions.Select(GetFieldDeclaration).ToArray();

            MemberDeclarationSyntax GetFieldDeclaration(InvokerFieldDescripton description)
            {
                var field = FieldDeclaration(
                    VariableDeclaration(
                        description.FieldType.ToTypeSyntax(),
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
            List<InvokerFieldDescripton> fieldDescriptions)
        {
            var injected = fieldDescriptions.Where(f => f.IsInjected).ToList();
            var parameters = injected.Select(
                f => Parameter(f.FieldName.ToIdentifier()).WithType(f.FieldType.ToTypeSyntax()));
            var body = injected.Select(
                f => (StatementSyntax)ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        ThisExpression().Member(f.FieldName.ToIdentifierName()),
                        Unwrapped(f.FieldName.ToIdentifierName()))));
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
            IInvokableInterfaceDescription interfaceDescription)
        {
            var fields = new List<InvokerFieldDescripton>();

            ushort fieldId = 0;
            foreach (var parameter in method.Method.Parameters)
            {
                fields.Add(new MethodParameterFieldDescription(method, parameter, $"arg{fieldId}", fieldId));
                fieldId++;
            }

            fields.Add(new TargetFieldDescription(interfaceDescription.InterfaceType));

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
            public TargetFieldDescription(ITypeSymbol fieldType) : base(fieldType, "target")
            {
            }

            public override bool IsInjected => false;
            public override bool IsSerializable => false;
            public override TypeSyntax FieldTypeSyntax => FieldType.ToTypeSyntax();
        }

        internal class MethodParameterFieldDescription : InvokerFieldDescripton, IMemberDescription
        {
            public MethodParameterFieldDescription(MethodDescription method, IParameterSymbol parameter, string fieldName, ushort fieldId)
                : base(parameter.Type, fieldName)
            {
                FieldId = fieldId;
                Parameter = parameter;
                if (parameter.Type.TypeKind == TypeKind.Dynamic)
                {
                    TypeSyntax = PredefinedType(Token(SyntaxKind.ObjectKeyword));
                }
                else if (parameter.Type is ITypeParameterSymbol)
                {
                    var match = method.AllTypeParameters.First(p => SymbolEqualityComparer.Default.Equals(parameter.Type, p.Parameter));
                    TypeSyntax = IdentifierName(match.Name);
                }
                else
                {
                    TypeSyntax = Type.ToTypeSyntax();
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
        }
    }
}