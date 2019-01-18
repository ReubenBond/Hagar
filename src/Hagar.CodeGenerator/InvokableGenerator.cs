using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    /// <summary>
    /// Generates RPC stub objects called invokers.
    /// </summary>
    internal static class InvokableGenerator
    {
        public static (ClassDeclarationSyntax, IGeneratedInvokerDescription) Generate(
            Compilation compilation,
            LibraryTypes libraryTypes,
            IInvokableInterfaceDescription interfaceDescription,
            MethodDescription methodDescription)
        {
            var method = methodDescription.Method;
            var generatedClassName = GetSimpleClassName(method);

            var fieldDescriptions = GetFieldDescriptions(methodDescription.Method, interfaceDescription);
            var fields = GetFieldDeclarations(fieldDescriptions, libraryTypes);
            var ctor = GenerateConstructor(generatedClassName, fieldDescriptions);

            var targetField = fieldDescriptions.OfType<TargetFieldDescription>().Single();
            var resultField = fieldDescriptions.OfType<ResultFieldDescription>().FirstOrDefault();

            var classDeclaration = ClassDeclaration(generatedClassName)
                .AddBaseListTypes(SimpleBaseType(libraryTypes.Invokable.ToTypeSyntax()))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(
                    AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fields)
                .AddMembers(ctor)
                .AddMembers(
                    GenerateGetArgumentCount(libraryTypes, methodDescription),
                    GenerateSetTargetMethod(libraryTypes, interfaceDescription, targetField),
                    GenerateGetTargetMethod(libraryTypes, targetField),
                    GenerateResetMethod(libraryTypes, fieldDescriptions),
                    GenerateGetArgumentMethod(libraryTypes, methodDescription, fieldDescriptions),
                    GenerateSetArgumentMethod(libraryTypes, methodDescription, fieldDescriptions),
                    GenerateInvokeMethod(libraryTypes, methodDescription, fieldDescriptions, targetField, resultField),
                    GenerateSetResultProperty(libraryTypes, resultField),
                    GenerateGetResultProperty(libraryTypes, resultField));

            var typeParameters = interfaceDescription.InterfaceType.TypeParameters.Select(tp => (tp, tp.Name))
                .Concat(method.TypeParameters.Select(tp => (tp, tp.Name)))
                .ToList();
            if (typeParameters.Count > 0)
            {
                classDeclaration = AddGenericTypeConstraints(classDeclaration, typeParameters);
            }

            return (classDeclaration,
                new GeneratedInvokerDescription(
                    interfaceDescription,
                    methodDescription,
                    generatedClassName,
                    fieldDescriptions.OfType<IMemberDescription>().ToList()));
        }

        private static MemberDeclarationSyntax GenerateSetTargetMethod(
            LibraryTypes libraryTypes,
            IInvokableInterfaceDescription interfaceDescription,
            TargetFieldDescription targetField)
        {
            var type = IdentifierName("TTargetHolder");
            var typeToken = Identifier("TTargetHolder");
            var holderParameter = Identifier("holder");
            var holder = IdentifierName("holder");

            var getTarget = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        holder,
                        GenericName(interfaceDescription.IsExtension ? "GetExtension" : "GetTarget")
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
            LibraryTypes libraryTypes,
            TargetFieldDescription targetField)
        {
            var type = IdentifierName("TTarget");
            var typeToken = Identifier("TTarget");

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
            List<FieldDescription> fields)
        {
            var index = IdentifierName("index");
            var type = IdentifierName("TArgument");
            var typeToken = Identifier("TArgument");

            var cases = new List<SwitchSectionSyntax>();
            foreach (var field in fields)
            {
                if (!(field is MethodParameterFieldDescription parameter)) continue;

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
            var body = SwitchStatement(index, new SyntaxList<SwitchSectionSyntax>(cases));
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
            List<FieldDescription> fields)
        {
            var index = IdentifierName("index");
            var value = IdentifierName("value");
            var type = IdentifierName("TArgument");
            var typeToken = Identifier("TArgument");

            var cases = new List<SwitchSectionSyntax>();
            foreach (var field in fields)
            {
                if (!(field is MethodParameterFieldDescription parameter)) continue;

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
            var body = SwitchStatement(index, new SyntaxList<SwitchSectionSyntax>(cases));
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

        private static MemberDeclarationSyntax GenerateInvokeMethod(
            LibraryTypes libraryTypes,
            MethodDescription method,
            List<FieldDescription> fields,
            TargetFieldDescription target,
            ResultFieldDescription result)
        {
            var body = new List<StatementSyntax>();

            var resultTask = IdentifierName("resultTask");

            // C# var resultTask = this.target.{Method}({params});
            var args = SeparatedList(
                fields.OfType<MethodParameterFieldDescription>()
                    .OrderBy(p => p.ParameterOrdinal)
                    .Select(p => Argument(ThisExpression().Member(p.FieldName))));
            ExpressionSyntax methodCall;
            if (method.Method.TypeParameters.Length > 0)
            {
                methodCall = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ThisExpression().Member(target.FieldName),
                    GenericName(
                        Identifier(method.Method.Name),
                        TypeArgumentList(
                            SeparatedList<TypeSyntax>(
                                method.Method.TypeParameters.Select(p => IdentifierName(p.Name))))));
            }
            else
            {
                methodCall = ThisExpression().Member(target.FieldName).Member(method.Method.Name);
            }
            
            body.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        ParseTypeName("var"),
                        SingletonSeparatedList(
                            VariableDeclarator(resultTask.Identifier)
                                .WithInitializer(
                                    EqualsValueClause(
                                        InvocationExpression(
                                            methodCall,
                                            ArgumentList(args))))))));

            // C#: if (resultTask.IsCompleted) // Even if it failed.
            // C#: {
            // C#:     this.result = resultTask.GetAwaiter().GetResult();
            // C#:     return default; // default(ValueTask) is a successfully completed ValueTask.
            // C#: }
            var synchronousCompletionBody = new List<StatementSyntax>();
            if (result != null)
            {
                synchronousCompletionBody.Add(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            ThisExpression().Member(result.FieldName),
                            InvocationExpression(
                                InvocationExpression(resultTask.Member("GetAwaiter")).Member("GetResult")))));
            }

            synchronousCompletionBody.Add(ReturnStatement(DefaultExpression(libraryTypes.ValueTask.ToTypeSyntax())));
            body.Add(IfStatement(resultTask.Member("IsCompleted"), Block(synchronousCompletionBody)));

            // C#: async ValueTask InvokeAsync(ValueTask<int> asyncValue)
            // C#: {
            // C#:     this.result = await asyncValue.ConfigureAwait(false);
            // C#: }
            var invokeAsyncParam = IdentifierName("asyncTask");
            var invokeAsyncBody = new List<StatementSyntax>();
            var awaitExpression = AwaitExpression(
                InvocationExpression(
                    invokeAsyncParam.Member("ConfigureAwait"),
                    ArgumentList(
                        SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.FalseLiteralExpression))))));
            if (result != null)
            {
                invokeAsyncBody.Add(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            ThisExpression().Member(result.FieldName),
                            awaitExpression)));
            }
            else
            {
                invokeAsyncBody.Add(ExpressionStatement(AwaitExpression(invokeAsyncParam)));
            }

            var invokeAsync = LocalFunctionStatement(libraryTypes.ValueTask.ToTypeSyntax(), "InvokeAsync")
                .WithModifiers(TokenList(Token(SyntaxKind.AsyncKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(invokeAsyncParam.Identifier).WithType(method.Method.ReturnType.ToTypeSyntax()))))
                .WithBody(Block(invokeAsyncBody));

            // C#: return InvokeAsync(resultTask);
            body.Add(
                ReturnStatement(
                    InvocationExpression(
                        IdentifierName("InvokeAsync"),
                        ArgumentList(SingletonSeparatedList(Argument(resultTask))))));
            body.Add(invokeAsync);

            return MethodDeclaration(libraryTypes.ValueTask.ToTypeSyntax(), "Invoke")
                .WithParameterList(ParameterList())
                .WithBody(Block(body))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateResetMethod(
            LibraryTypes libraryTypes,
            List<FieldDescription> fields)
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

            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "Reset")
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

        private static MemberDeclarationSyntax GenerateResultProperty(
            LibraryTypes libraryTypes,
            ResultFieldDescription resultField)
        {
            var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithExpressionBody(
                    ArrowExpressionClause(
                        CastExpression(
                            libraryTypes.Object.ToTypeSyntax(),
                            ThisExpression().Member(resultField.FieldName))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            var setter = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithExpressionBody(
                    ArrowExpressionClause(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            ThisExpression().Member(resultField.FieldName),
                            CastExpression(resultField.FieldType.ToTypeSyntax(), IdentifierName("value")))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            return PropertyDeclaration(libraryTypes.Object.ToTypeSyntax(), "Result")
                .WithAccessorList(
                    AccessorList()
                        .AddAccessors(
                            getter,
                            setter))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)));
        }

        private static MemberDeclarationSyntax GenerateSetResultProperty(
            LibraryTypes libraryTypes,
            ResultFieldDescription resultField)
        {

            var type = IdentifierName("TResult");
            var typeToken = Identifier("TResult");

            ExpressionSyntax body;
            if (resultField != null)
            {
                body = AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    ThisExpression().Member(resultField.FieldName),
                    CastExpression(
                        resultField.FieldType.ToTypeSyntax(),
                        CastExpression(libraryTypes.Object.ToTypeSyntax(), IdentifierName("value"))));
            }
            else
            {
                body = ThrowExpression(
                    ObjectCreationExpression(libraryTypes.InvalidOperationException.ToTypeSyntax())
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument("Method does not have a return value.".GetLiteralExpression())))));
            }

            return MethodDeclaration(libraryTypes.Void.ToTypeSyntax(), "SetResult")
                .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(TypeParameter(typeToken))))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("value"))
                                .WithType(type)
                                .WithModifiers(TokenList(Token(SyntaxKind.InKeyword))))))
                .WithExpressionBody(ArrowExpressionClause(body))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private static MemberDeclarationSyntax GenerateGetResultProperty(
            LibraryTypes libraryTypes,
            ResultFieldDescription resultField)
        {

            var type = IdentifierName("TResult");
            var typeToken = Identifier("TResult");

            ExpressionSyntax body;
            if (resultField != null)
            {
                body = CastExpression(
                    type,
                    CastExpression(libraryTypes.Object.ToTypeSyntax(), ThisExpression().Member(resultField.FieldName)));
            }
            else
            {
                body = ThrowExpression(
                    ObjectCreationExpression(libraryTypes.InvalidOperationException.ToTypeSyntax())
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument("Method does not have a return value.".GetLiteralExpression())))));
            }

            return MethodDeclaration(type, "GetResult")
                .WithTypeParameterList(TypeParameterList(SingletonSeparatedList(TypeParameter(typeToken))))
                .WithParameterList(ParameterList())
                .WithExpressionBody(ArrowExpressionClause(body))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private class GeneratedInvokerDescription : IGeneratedInvokerDescription
        {
            private readonly MethodDescription methodDescription;

            public GeneratedInvokerDescription(
                IInvokableInterfaceDescription interfaceDescription,
                MethodDescription methodDescription,
                string generatedClassName,
                List<IMemberDescription> members)
            {
                this.InterfaceDescription = interfaceDescription;
                this.methodDescription = methodDescription;
                this.Name = generatedClassName;
                this.Members = members;

                this.TypeParameters = interfaceDescription.InterfaceType.TypeParameters
                    .Concat(this.methodDescription.Method.TypeParameters)
                    .ToImmutableArray();
            }

            public TypeSyntax TypeSyntax
            {
                get
                {
                    var name = GetSimpleClassName(this.methodDescription.Method);

                    if (this.TypeParameters.Length > 0)
                    {
                        return GenericName(
                            Identifier(name),
                            TypeArgumentList(
                                SeparatedList<TypeSyntax>(this.TypeParameters.Select(p => IdentifierName(p.Name)))));
                    }

                    return IdentifierName(name);
                }
            }

            public TypeSyntax UnboundTypeSyntax => this.methodDescription.GetInvokableTypeName();
            public bool HasComplexBaseType => false;
            public INamedTypeSymbol BaseType => throw new NotImplementedException();
            public string Name { get; }
            public bool IsValueType => false;
            public bool IsGenericType => this.methodDescription.Method.IsGenericMethod;
            public ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }
            public List<IMemberDescription> Members { get; }
            public IInvokableInterfaceDescription InterfaceDescription { get; }
        }

        public static string GetSimpleClassName(IMethodSymbol method)
        {
            var typeArgs = method.TypeParameters.Length > 0 ? "_" + method.TypeParameters.Length : string.Empty;
            var args = method.Parameters.Length > 0
                ? "_" + string.Join("_", method.Parameters.Select(p => p.Type.Name))
                : string.Empty;
            return
                $"{CodeGenerator.CodeGeneratorName}_Invokable_{method.ContainingType.Name}_{method.Name}{typeArgs}{args}";
        }

        private static ClassDeclarationSyntax AddGenericTypeConstraints(
            ClassDeclarationSyntax classDeclaration,
            List<(ITypeParameterSymbol, string)> typeParameters)
        {
            classDeclaration = classDeclaration.WithTypeParameterList(
                TypeParameterList(SeparatedList(typeParameters.Select(tp => TypeParameter(tp.Item2)))));
            var constraints = new List<TypeParameterConstraintSyntax>();
            foreach (var (tp, _) in typeParameters)
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
                    classDeclaration = classDeclaration.AddConstraintClauses(
                        TypeParameterConstraintClause(tp.Name).AddConstraints(constraints.ToArray()));
                }
            }

            return classDeclaration;
        }

        private static MemberDeclarationSyntax[] GetFieldDeclarations(
            List<FieldDescription> fieldDescriptions,
            LibraryTypes libraryTypes)
        {
            return fieldDescriptions.Select(GetFieldDeclaration).ToArray();

            MemberDeclarationSyntax GetFieldDeclaration(FieldDescription description)
            {
                var field = FieldDeclaration(
                    VariableDeclaration(
                        description.FieldType.ToTypeSyntax(),
                        SingletonSeparatedList(VariableDeclarator(description.FieldName))));

                switch (description)
                {
                    case ResultFieldDescription _:
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
                                    libraryTypes.IdAttribute.ToNameSyntax(),
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
            List<FieldDescription> fieldDescriptions)
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

            ExpressionSyntax Unwrapped(ExpressionSyntax expr)
            {
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("HagarGeneratedCodeHelper"),
                        IdentifierName("UnwrapService")),
                    ArgumentList(SeparatedList(new[] {Argument(ThisExpression()), Argument(expr)})));
            }
        }

        private static List<FieldDescription> GetFieldDescriptions(
            IMethodSymbol method,
            IInvokableInterfaceDescription interfaceDescription)
        {
            var fields = new List<FieldDescription>();

            uint fieldId = 0;
            foreach (var parameter in method.Parameters)
            {
                fields.Add(new MethodParameterFieldDescription(parameter, $"arg{fieldId}", fieldId));
                fieldId++;
            }

            if (method.ReturnType is INamedTypeSymbol returnType && returnType.TypeArguments.Length == 1)
            {
                fields.Add(new ResultFieldDescription(returnType.TypeArguments[0]));
            }

            fields.Add(new TargetFieldDescription(interfaceDescription.InterfaceType));

            return fields;
        }

        internal abstract class FieldDescription
        {
            protected FieldDescription(ITypeSymbol fieldType, string fieldName)
            {
                this.FieldType = fieldType;
                this.FieldName = fieldName;
            }

            public ITypeSymbol FieldType { get; }
            public string FieldName { get; }
            public abstract bool IsInjected { get; }
            public abstract bool IsSerializable { get; }
        }

        internal class InjectedFieldDescription : FieldDescription
        {
            public InjectedFieldDescription(ITypeSymbol fieldType, string fieldName) : base(fieldType, fieldName)
            {
            }

            public override bool IsInjected => true;
            public override bool IsSerializable => false;
        }

        internal class CodecFieldDescription : FieldDescription, ICodecDescription
        {
            public CodecFieldDescription(ITypeSymbol fieldType, string fieldName, ITypeSymbol underlyingType) : base(
                fieldType,
                fieldName)
            {
                this.UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => true;
            public override bool IsSerializable => false;
        }

        internal class TypeFieldDescription : FieldDescription
        {
            public TypeFieldDescription(ITypeSymbol fieldType, string fieldName, ITypeSymbol underlyingType) : base(
                fieldType,
                fieldName)
            {
                this.UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => false;
            public override bool IsSerializable => false;
        }

        internal class ResultFieldDescription : FieldDescription
        {
            public ResultFieldDescription(ITypeSymbol fieldType) : base(fieldType, "result")
            {
            }

            public override bool IsInjected => false;
            public override bool IsSerializable => false;
        }

        internal class TargetFieldDescription : FieldDescription
        {
            public TargetFieldDescription(ITypeSymbol fieldType) : base(fieldType, "target")
            {
            }

            public override bool IsInjected => false;
            public override bool IsSerializable => false;
        }

        internal class MethodParameterFieldDescription : FieldDescription, IMemberDescription
        {
            public MethodParameterFieldDescription(IParameterSymbol parameter, string fieldName, uint fieldId)
                : base(parameter.Type, fieldName)
            {
                this.FieldId = fieldId;
                this.Parameter = parameter;
            }

            public int ParameterOrdinal => this.Parameter.Ordinal;

            public override bool IsInjected => false;
            public uint FieldId { get; }
            public ISymbol Member => this.Parameter;
            public ITypeSymbol Type => this.FieldType;
            public IParameterSymbol Parameter { get; }
            public string Name => this.FieldName;
            public override bool IsSerializable => true;
        }
    }
}