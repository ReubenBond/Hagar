using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Hagar.CodeGenerator.InvokableGenerator;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal static class CopierGenerator
    {
        private const string BaseTypeCopierFieldName = "_baseTypeCopier";
        private const string ActivatorFieldName = "_activator";
        private const string DeepCopyMethodName = "DeepCopy";

        public static ClassDeclarationSyntax GenerateCopier(
            LibraryTypes libraryTypes,
            ISerializableTypeDescription type)
        {
            var simpleClassName = GetSimpleClassName(type);

            var members = new List<ISerializableMember>();
            foreach (var member in type.Members)
            {
                if (member is IFieldDescription)
                {
                    members.Add(new SerializableMember(libraryTypes, type, member, members.Count));
                }
                else if (member is MethodParameterFieldDescription methodParameter)
                {
                    members.Add(new SerializableMethodMember(libraryTypes, methodParameter, members.Count));
                }
            }

            var fieldDescriptions = GetFieldDescriptions(type, members, libraryTypes);
            var fieldDeclarations = GetFieldDeclarations(fieldDescriptions);
            var ctor = GenerateConstructor(libraryTypes, simpleClassName, fieldDescriptions);

            var accessibility = type.Accessibility switch
            {
                Accessibility.Public => SyntaxKind.PublicKeyword,
                _ => SyntaxKind.InternalKeyword,
            };
            var classDeclaration = ClassDeclaration(simpleClassName)
                .AddBaseListTypes(SimpleBaseType(libraryTypes.DeepCopier_1.ToTypeSyntax(type.TypeSyntax)))
                .AddModifiers(Token(accessibility), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fieldDeclarations)
                .AddMembers(ctor);

            if (type.IsEnumType)
            {
                var copyMethod = GenerateEnumCopyMethod(type, libraryTypes);
                classDeclaration = classDeclaration.AddMembers(copyMethod);
            }
            else
            {
                var copyMethod = GenerateDeepCopyMethod(type, fieldDescriptions, members, libraryTypes);
                classDeclaration = classDeclaration.AddMembers(copyMethod);
            }

            if (type.IsGenericType)
            {
                classDeclaration = AddGenericTypeParameters(classDeclaration, type);
            }

            return classDeclaration;
        }

        public static string GetSimpleClassName(ISerializableTypeDescription serializableType) => GetSimpleClassName(serializableType.Name);

        public static string GetSimpleClassName(string name) => $"Copier_{name}";

        public static string GetGeneratedNamespaceName(ITypeSymbol type) => $"{CodeGenerator.CodeGeneratorName}.{type.GetNamespaceAndNesting()}";

        private static ClassDeclarationSyntax AddGenericTypeParameters(ClassDeclarationSyntax classDeclaration, ISerializableTypeDescription serializableType)
        {
            classDeclaration = classDeclaration.WithTypeParameterList(TypeParameterList(SeparatedList(serializableType.TypeParameters.Select(tp => TypeParameter(tp.Name)))));
            var constraints = new List<TypeParameterConstraintSyntax>();
            foreach (var tp in serializableType.TypeParameters)
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

        private static MemberDeclarationSyntax[] GetFieldDeclarations(List<FieldDescription> fieldDescriptions)
        {
            return fieldDescriptions.Select(GetFieldDeclaration).ToArray();

            static MemberDeclarationSyntax GetFieldDeclaration(FieldDescription description)
            {
                switch (description)
                {
                    case TypeFieldDescription type:
                        return FieldDeclaration(
                                VariableDeclaration(
                                    type.FieldType,
                                    SingletonSeparatedList(VariableDeclarator(type.FieldName)
                                        .WithInitializer(EqualsValueClause(TypeOfExpression(type.UnderlyingType.ToTypeSyntax()))))))
                            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));
                    case SetterFieldDescription setter:
                        {
                            var fieldSetterVariable = VariableDeclarator(setter.FieldName);

                            return
                                FieldDeclaration(VariableDeclaration(setter.FieldType).AddVariables(fieldSetterVariable))
                                    .AddModifiers(
                                        Token(SyntaxKind.PrivateKeyword),
                                        Token(SyntaxKind.ReadOnlyKeyword));
                        }
                    case GetterFieldDescription getter:
                        {
                            var fieldGetterVariable = VariableDeclarator(getter.FieldName);

                            return
                                FieldDeclaration(VariableDeclaration(getter.FieldType).AddVariables(fieldGetterVariable))
                                    .AddModifiers(
                                        Token(SyntaxKind.PrivateKeyword),
                                        Token(SyntaxKind.ReadOnlyKeyword));
                        }
                    default:
                        return FieldDeclaration(VariableDeclaration(description.FieldType, SingletonSeparatedList(VariableDeclarator(description.FieldName))))
                            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));
                }
            }
        }

        private static ConstructorDeclarationSyntax GenerateConstructor(LibraryTypes libraryTypes, string simpleClassName, List<FieldDescription> fieldDescriptions)
        {
            var injected = fieldDescriptions.Where(f => f.IsInjected).ToList();
            var parameters = new List<ParameterSyntax>(injected.Select(f => Parameter(f.FieldName.ToIdentifier()).WithType(f.FieldType)));
            const string CodecProviderParameterName = "codecProvider";
            parameters.Add(Parameter(Identifier(CodecProviderParameterName)).WithType(libraryTypes.ICodecProvider.ToTypeSyntax()));

            var fieldAccessorUtility = AliasQualifiedName("global", IdentifierName("Hagar")).Member("Utilities").Member("FieldAccessor");

            IEnumerable<StatementSyntax> GetStatements()
            {
                foreach (var field in fieldDescriptions)
                {
                    switch (field)
                    {
                        case GetterFieldDescription getter:
                            yield return InitializeGetterField(getter);
                            break;

                        case SetterFieldDescription setter:
                            yield return InitializeSetterField(setter);
                            break;

                        case FieldDescription _ when field.IsInjected:
                            yield return ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    ThisExpression().Member(field.FieldName.ToIdentifierName()),
                                    Unwrapped(field.FieldName.ToIdentifierName())));
                            break;
                        case CopierFieldDescription codec when !field.IsInjected:
                            {
                                yield return ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        ThisExpression().Member(field.FieldName.ToIdentifierName()),
                                        GetService(field.FieldType)));
                            }
                            break;
                    }
                }
            }

            StatementSyntax InitializeGetterField(GetterFieldDescription getter)
            {
                var fieldInfo = GetFieldInfo(getter.Member.Field.ContainingType, getter.Member.Field.Name);
                var accessorInvoke = CastExpression(
                    getter.FieldType,
                    InvocationExpression(fieldAccessorUtility.Member("GetGetter")).AddArgumentListArguments(Argument(fieldInfo)));

                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(getter.FieldName), accessorInvoke));
            }

            StatementSyntax InitializeSetterField(SetterFieldDescription setter)
            {
                var field = setter.Member.Field;
                var fieldInfo = GetFieldInfo(field.ContainingType, field.Name);
                var accessorMethod = setter.IsContainedByValueType ? "GetValueSetter" : "GetReferenceSetter";
                var accessorInvoke = CastExpression(
                    setter.FieldType,
                    InvocationExpression(fieldAccessorUtility.Member(accessorMethod))
                        .AddArgumentListArguments(Argument(fieldInfo)));

                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(setter.FieldName), accessorInvoke));
            }

            static InvocationExpressionSyntax GetFieldInfo(INamedTypeSymbol containingType, string fieldName)
            {
                var bindingFlags = SymbolSyntaxExtensions.GetBindingFlagsParenthesizedExpressionSyntax(
                    SyntaxKind.BitwiseOrExpression,
                    BindingFlags.Instance,
                    BindingFlags.NonPublic,
                    BindingFlags.Public);
                return InvocationExpression(TypeOfExpression(containingType.ToTypeSyntax()).Member("GetField"))
                            .AddArgumentListArguments(
                                Argument(fieldName.GetLiteralExpression()),
                                Argument(bindingFlags));
            }

            return ConstructorDeclaration(simpleClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters.ToArray())
                .AddBodyStatements(GetStatements().ToArray());

            static ExpressionSyntax Unwrapped(ExpressionSyntax expr)
            {
                return InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("HagarGeneratedCodeHelper"), IdentifierName("UnwrapService")),
                    ArgumentList(SeparatedList(new[] { Argument(ThisExpression()), Argument(expr) })));
            }

            static ExpressionSyntax GetService(TypeSyntax type)
            {
                return InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("HagarGeneratedCodeHelper"), GenericName(Identifier("GetService"), TypeArgumentList(SingletonSeparatedList(type)))),
                    ArgumentList(SeparatedList(new[] { Argument(ThisExpression()), Argument(IdentifierName(CodecProviderParameterName)) })));
            }
        }

        private static List<FieldDescription> GetFieldDescriptions(
            ISerializableTypeDescription serializableTypeDescription,
            List<ISerializableMember> members,
            LibraryTypes libraryTypes)
        {
            var fields = new List<FieldDescription>();
#pragma warning disable RS1024 // Compare symbols correctly
            fields.AddRange(serializableTypeDescription.Members.Select(m => GetExpectedType(m.Type)).Distinct(SymbolEqualityComparer.Default).OfType<ITypeSymbol>().Select(GetTypeDescription));
#pragma warning restore RS1024 // Compare symbols correctly

            if (serializableTypeDescription.HasComplexBaseType)
            {
                fields.Add(new PartialCopierFieldDescription(libraryTypes.PartialCopier.Construct(serializableTypeDescription.BaseType).ToTypeSyntax(), BaseTypeCopierFieldName));
            }

            if (serializableTypeDescription.UseActivator)
            {
                fields.Add(new ActivatorFieldDescription(libraryTypes.IActivator_1.ToTypeSyntax(serializableTypeDescription.TypeSyntax), ActivatorFieldName));
            }

            // Add a codec field for any field in the target which does not have a static codec.
#pragma warning disable RS1024 // Compare symbols correctly
            fields.AddRange(serializableTypeDescription.Members
                .Select(m => GetExpectedType(m.Type)).Distinct(SymbolEqualityComparer.Default)
#pragma warning restore RS1024 // Compare symbols correctly
                .Cast<ITypeSymbol>()
                .Where(t => !libraryTypes.StaticCopiers.Any(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, t)))
                .Select(type => GetCopierDescription(type)));

            foreach (var member in members)
            {
                if (!member.HasAccessibleGetter)
                {
                    fields.Add(GetGetterDescription(member));
                }

                if (!member.HasAccessibleSetter)
                {
                    fields.Add(GetSetterDescription(member));
                }
            }

            return fields;

            CopierFieldDescription GetCopierDescription(ITypeSymbol t)
            {
                TypeSyntax copierType = null;
                if (t.HasAttribute(libraryTypes.GenerateSerializerAttribute)
                    && (!SymbolEqualityComparer.Default.Equals(t.ContainingAssembly, libraryTypes.Compilation.Assembly) || t.ContainingAssembly.HasAttribute(libraryTypes.MetadataProviderAttribute)))
                {
                    // Use the concrete generated type and avoid expensive interface dispatch
                    if (t is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
                    {
                        // Construct the full generic type name
                        var ns = GetGeneratedNamespaceName(t);
                        var name = GenericName(Identifier(GetSimpleClassName(t.Name)), TypeArgumentList(SeparatedList(namedTypeSymbol.TypeArguments.Select(arg => arg.ToTypeSyntax()))));
                        copierType = QualifiedName(ParseName(ns), name);
                    }
                    else
                    {
                        var simpleName = $"{GetGeneratedNamespaceName(t)}.{GetSimpleClassName(t.Name)}";
                        copierType = ParseTypeName(simpleName);
                    }
                }
                else if (libraryTypes.WellKnownCopiers.FirstOrDefault(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, t)) is WellKnownCopierDescription codec)
                {
                    // The codec is not a static copier and is also not a generic copiers.
                    copierType = codec.CopierType.ToTypeSyntax();
                }
                else if (t is INamedTypeSymbol named && libraryTypes.WellKnownCopiers.FirstOrDefault(c => t is INamedTypeSymbol named && named.ConstructedFrom is ISymbol unboundFieldType && SymbolEqualityComparer.Default.Equals(c.UnderlyingType, unboundFieldType)) is WellKnownCopierDescription genericCopier)
                {
                    // Construct the generic copier type using the field's type arguments.
                    copierType = genericCopier.CopierType.Construct(named.TypeArguments.ToArray()).ToTypeSyntax();
                }
                else
                {
                    // Use the IDeepCopier<T> interface
                    copierType = libraryTypes.DeepCopier_1.Construct(t).ToTypeSyntax();
                }

                var fieldName = '_' + ToLowerCamelCase(t.GetValidIdentifier()) + "Copier";
                return new CopierFieldDescription(copierType, fieldName, t);
            }

            TypeFieldDescription GetTypeDescription(ITypeSymbol t)
            {
                var fieldName = '_' + ToLowerCamelCase(t.GetValidIdentifier()) + "Type";
                return new TypeFieldDescription(libraryTypes.Type.ToTypeSyntax(), fieldName, t);
            }

            GetterFieldDescription GetGetterDescription(ISerializableMember member)
            {
                var containingType = member.Field.ContainingType;
                var getterType = libraryTypes.Func_2.Construct(containingType, member.SafeType).ToTypeSyntax();
                return new GetterFieldDescription(getterType, member.GetterFieldName, member.Field.Type, member);
            }

            SetterFieldDescription GetSetterDescription(ISerializableMember member)
            {
                var containingType = member.Field.ContainingType;
                TypeSyntax fieldType;
                if (containingType != null && containingType.IsValueType)
                {
                    fieldType = libraryTypes.ValueTypeSetter_2.Construct(containingType, member.SafeType).ToTypeSyntax();
                }
                else
                {
                    fieldType = libraryTypes.Action_2.Construct(containingType, member.SafeType).ToTypeSyntax();
                }

                return new SetterFieldDescription(fieldType, member.SetterFieldName, member.Field.Type, member);
            }

            static string ToLowerCamelCase(string input) => char.IsLower(input, 0) ? input : char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Returns the "expected" type for <paramref name="type"/> which is used for selecting the correct copier.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static ITypeSymbol GetExpectedType(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol)
            {
                return type;
            }

            if (type is IPointerTypeSymbol pointerType)
            {
                throw new NotSupportedException($"Cannot serialize pointer type {pointerType.Name}");
            }

            return type;
        }

        private static MemberDeclarationSyntax GenerateDeepCopyMethod(
            ISerializableTypeDescription type,
            List<FieldDescription> copierFields,
            List<ISerializableMember> members,
            LibraryTypes libraryTypes)
        {
            var returnType = type.TypeSyntax;

            var originalParam = "original".ToIdentifierName();
            var contextParam = "context".ToIdentifierName();
            var resultVar = "result".ToIdentifierName();

            var body = new List<StatementSyntax>();
            
            ExpressionSyntax createValueExpression = type.UseActivator switch
            {
                true => InvocationExpression(copierFields.OfType<ActivatorFieldDescription>().Single().FieldName.ToIdentifierName().Member("Create")),
                false => type.GetObjectCreationExpression(libraryTypes)
            };

            if (!type.IsValueType)
            {
                // C#: if (context.TryGetCopy(original, out T result)) { return result; }
                var tryGetCopy = InvocationExpression(
                    contextParam.Member("TryGetCopy"),
                    ArgumentList(SeparatedList(new[]
                    {
                        Argument(originalParam),
                        Argument(DeclarationExpression(
                            type.TypeSyntax,
                            SingleVariableDesignation(Identifier("result"))))
                                    .WithRefKindKeyword(Token(SyntaxKind.OutKeyword))
                    })));
                body.Add(IfStatement(tryGetCopy, ReturnStatement(resultVar)));

                if (!type.IsSealedType)
                {
                    // C#: if (original.GetType() != typeof(<codec>)) { return context.Copy(original); }
                    var exactTypeMatch = BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, originalParam, IdentifierName("GetType"))),
                            TypeOfExpression(type.TypeSyntax));
                    var contextCopy = InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, contextParam, IdentifierName("Copy")))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(originalParam))));
                    body.Add(IfStatement(exactTypeMatch, ReturnStatement(contextCopy)));
                }

                // C#: result = _activator.Create();
                body.Add(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, resultVar, createValueExpression)));

                // C#: context.RecordCopy(original, result);
                body.Add(ExpressionStatement(InvocationExpression(contextParam.Member("RecordCopy"), ArgumentList(SeparatedList(new[]
                {
                    Argument(originalParam),
                    Argument(resultVar)
                })))));

                if (type.HasComplexBaseType)
                {
                    // C#: _baseTypeCopier.DeepCopy(original, result, context);
                    body.Add(
                        ExpressionStatement(
                            InvocationExpression(
                                ThisExpression().Member(BaseTypeCopierFieldName.ToIdentifierName()).Member(DeepCopyMethodName),
                                ArgumentList(SeparatedList(new[]
                                {
                                    Argument(originalParam),
                                    Argument(resultVar),
                                    Argument(contextParam)
                                })))));
                }
            }
            else
            {
                // C#: TField result = _activator.Create();
                // or C#: TField result = new TField();
                body.Add(LocalDeclarationStatement(
                    VariableDeclaration(
                        type.TypeSyntax,
                        SingletonSeparatedList(VariableDeclarator(resultVar.Identifier)
                        .WithInitializer(EqualsValueClause(createValueExpression))))));

            }

            var codecs = copierFields.OfType<ICopierDescription>()
                    .Concat(libraryTypes.StaticCopiers)
                    .ToList();

            var orderedMembers = members.OrderBy(m => m.Description.FieldId).ToList();
            var lastMember = orderedMembers.LastOrDefault();

            foreach (var member in orderedMembers)
            {
                var description = member.Description;

                // Copiers can either be static classes or injected into the constructor.
                // Either way, the member signatures are the same.
                var codec = codecs.First(f => SymbolEqualityComparer.Default.Equals(f.UnderlyingType, GetExpectedType(description.Type)));
                var memberType = GetExpectedType(description.Type);
                var staticCopier = libraryTypes.StaticCopiers.FirstOrDefault(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, memberType));
                ExpressionSyntax codecExpression;
                if (staticCopier != null)
                {
                    codecExpression = staticCopier.CopierType.ToNameSyntax();
                }
                else
                {
                    var instanceCopier = copierFields.OfType<CopierFieldDescription>().First(f => SymbolEqualityComparer.Default.Equals(f.UnderlyingType, memberType));
                    codecExpression = ThisExpression().Member(instanceCopier.FieldName);
                }

                ExpressionSyntax getValueExpression;

                if (libraryTypes.IsShallowCopyable(member.SafeType))
                {
                    getValueExpression = member.GetGetter(originalParam);
                }
                else
                {
                    getValueExpression = InvocationExpression(
                        codecExpression.Member(DeepCopyMethodName),
                        ArgumentList(SeparatedList(new[] { Argument(member.GetGetter(originalParam)), Argument(contextParam) })));
                    if (!codec.UnderlyingType.Equals(member.Type))
                    {
                        // If the member type type differs from the codec type (eg because the member is an array), cast the result.
                        getValueExpression = CastExpression(description.Type.ToTypeSyntax(), getValueExpression);
                    }
                }

                var memberAssignment = ExpressionStatement(member.GetSetter(resultVar, getValueExpression));

                body.Add(memberAssignment);
            }

            body.Add(ReturnStatement(resultVar));

            var parameters = new[]
            {
                Parameter(originalParam.Identifier).WithType(type.TypeSyntax),
                Parameter(contextParam.Identifier).WithType(libraryTypes.CopyContext.ToTypeSyntax())
            };

            return MethodDeclaration(returnType, DeepCopyMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetMethodImplAttributeSyntax())))
                .AddBodyStatements(body.ToArray());
        }

        private static MemberDeclarationSyntax GenerateEnumCopyMethod(
            ISerializableTypeDescription type,
            LibraryTypes libraryTypes)
        {
            var returnType = type.TypeSyntax;

            var inputParam = "input".ToIdentifierName();

            var body = new StatementSyntax[]
            {
                ReturnStatement(inputParam)
            };

            var parameters = new[]
            {
                Parameter("input".ToIdentifier()).WithType(returnType),
                Parameter("_".ToIdentifier()).WithType(libraryTypes.CopyContext.ToTypeSyntax()),
            };

            return MethodDeclaration(returnType, DeepCopyMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetMethodImplAttributeSyntax())))
                .AddBodyStatements(body.ToArray());
        }

        internal abstract class FieldDescription
        {
            protected FieldDescription(TypeSyntax fieldType, string fieldName)
            {
                FieldType = fieldType;
                FieldName = fieldName;
            }

            public TypeSyntax FieldType { get; }
            public string FieldName { get; }
            public abstract bool IsInjected { get; }
        }

        internal class PartialCopierFieldDescription : FieldDescription
        {
            public PartialCopierFieldDescription(TypeSyntax fieldType, string fieldName) : base(fieldType, fieldName)
            {
            }

            public override bool IsInjected => true;
        }


        internal class ActivatorFieldDescription : FieldDescription 
        {
            public ActivatorFieldDescription(TypeSyntax fieldType, string fieldName) : base(fieldType, fieldName)
            {
            }

            public override bool IsInjected => true;
        }

        internal class CopierFieldDescription : FieldDescription, ICopierDescription
        {
            public CopierFieldDescription(TypeSyntax fieldType, string fieldName, ITypeSymbol underlyingType) : base(fieldType, fieldName)
            {
                UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => false;
        }

        internal class TypeFieldDescription : FieldDescription
        {
            public TypeFieldDescription(TypeSyntax fieldType, string fieldName, ITypeSymbol underlyingType) : base(fieldType, fieldName)
            {
                UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => false;
        }

        internal class SetterFieldDescription : FieldDescription
        {
            public SetterFieldDescription(TypeSyntax fieldType, string fieldName, ITypeSymbol targetFieldType, ISerializableMember member) : base(fieldType, fieldName)
            {
                TargetFieldType = targetFieldType;
                Member = member;
            }

            public ISerializableMember Member { get; }

            public ITypeSymbol TargetFieldType { get; }

            public override bool IsInjected => false;

            public bool IsContainedByValueType => Member.Field.ContainingType != null && Member.Field.ContainingType.IsValueType;
        }

        internal class GetterFieldDescription : FieldDescription
        {
            public GetterFieldDescription(TypeSyntax fieldType, string fieldName, ITypeSymbol targetFieldType, ISerializableMember member) : base(fieldType, fieldName)
            {
                TargetFieldType = targetFieldType;
                Member = member;
            }

            public ISerializableMember Member { get; }
            public ITypeSymbol TargetFieldType { get; }

            public override bool IsInjected => false;
        }

        internal interface ISerializableMember
        {
            IMemberDescription Description { get; }

            /// <summary>
            /// Gets the underlying <see cref="Field"/> instance.
            /// </summary>
            IFieldSymbol Field { get; }

            /// <summary>
            /// Gets a usable representation of the field type.
            /// </summary>
            /// <remarks>
            /// If the field is of type 'dynamic', we represent it as 'object' because 'dynamic' cannot appear in typeof expressions.
            /// </remarks>
            ITypeSymbol SafeType { get; }

            /// <summary>
            /// Gets the name of the getter field.
            /// </summary>
            string GetterFieldName { get; }

            /// <summary>
            /// Gets the name of the setter field.
            /// </summary>
            string SetterFieldName { get; }

            bool HasAccessibleGetter { get; }

            bool HasAccessibleSetter { get; }

            /// <summary>
            /// Gets syntax representing the type of this field.
            /// </summary>
            TypeSyntax Type { get; }

            /// <summary>
            /// Returns syntax for retrieving the value of this field, deep copying it if necessary.
            /// </summary>
            /// <param name="instance">The instance of the containing type.</param>
            /// <returns>Syntax for retrieving the value of this field.</returns>
            ExpressionSyntax GetGetter(ExpressionSyntax instance);

            /// <summary>
            /// Returns syntax for setting the value of this field.
            /// </summary>
            /// <param name="instance">The instance of the containing type.</param>
            /// <param name="value">Syntax for the new value.</param>
            /// <returns>Syntax for setting the value of this field.</returns>
            ExpressionSyntax GetSetter(ExpressionSyntax instance, ExpressionSyntax value);
        }

        /// <summary>
        /// Represents a serializable member (field/property) of a type.
        /// </summary>
        internal class SerializableMethodMember : ISerializableMember
        {
            private readonly MethodParameterFieldDescription _member;
            private readonly LibraryTypes _wellKnownTypes;

            /// <summary>
            /// The ordinal assigned to this field.
            /// </summary>
            private readonly int _ordinal;

            public SerializableMethodMember(LibraryTypes wellKnownTypes, MethodParameterFieldDescription member, int ordinal)
            {
                _member = member;
                _wellKnownTypes = wellKnownTypes;
                Description = member;
                _ordinal = ordinal;
            }

            public IMemberDescription Description { get; }

            /// <summary>
            /// Gets the underlying <see cref="Field"/> instance.
            /// </summary>
            public IFieldSymbol Field => throw new NotSupportedException();

            /// <summary>
            /// Gets a usable representation of the field type.
            /// </summary>
            /// <remarks>
            /// If the field is of type 'dynamic', we represent it as 'object' because 'dynamic' cannot appear in typeof expressions.
            /// </remarks>
            public ITypeSymbol SafeType => _member.FieldType.TypeKind == TypeKind.Dynamic
                ? _wellKnownTypes.Object
                : _member.FieldType;

            /// <summary>
            /// Gets the name of the getter field.
            /// </summary>
            public string GetterFieldName => "getField" + _ordinal;

            /// <summary>
            /// Gets the name of the setter field.
            /// </summary>
            public string SetterFieldName => "setField" + _ordinal;

            public bool HasAccessibleGetter => true;

            public bool HasAccessibleSetter => true;

            /// <summary>
            /// Gets syntax representing the type of this field.
            /// </summary>
            public TypeSyntax Type => SafeType.ToTypeSyntax();

            /// <summary>
            /// Returns syntax for retrieving the value of this field, deep copying it if necessary.
            /// </summary>
            /// <param name="instance">The instance of the containing type.</param>
            /// <returns>Syntax for retrieving the value of this field.</returns>
            public ExpressionSyntax GetGetter(ExpressionSyntax instance) => instance.Member(_member.FieldName);

            /// <summary>
            /// Returns syntax for setting the value of this field.
            /// </summary>
            /// <param name="instance">The instance of the containing type.</param>
            /// <param name="value">Syntax for the new value.</param>
            /// <returns>Syntax for setting the value of this field.</returns>
            public ExpressionSyntax GetSetter(ExpressionSyntax instance, ExpressionSyntax value) => AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        instance.Member(_member.FieldName),
                        value);
        }

        /// <summary>
        /// Represents a serializable member (field/property) of a type.
        /// </summary>
        internal class SerializableMember : ISerializableMember
        {
            private readonly SemanticModel _model;
            private readonly LibraryTypes _wellKnownTypes;
            private IPropertySymbol _property;

            /// <summary>
            /// The ordinal assigned to this field.
            /// </summary>
            private readonly int _ordinal;

            public SerializableMember(LibraryTypes wellKnownTypes, ISerializableTypeDescription type, IMemberDescription member, int ordinal)
            {
                _wellKnownTypes = wellKnownTypes;
                _model = type.SemanticModel;
                Description = member;
                _ordinal = ordinal;
            }

            public IMemberDescription Description { get; }

            /// <summary>
            /// Gets the underlying <see cref="Field"/> instance.
            /// </summary>
            public IFieldSymbol Field => (IFieldSymbol)Description.Member;

            /// <summary>
            /// Gets a usable representation of the field type.
            /// </summary>
            /// <remarks>
            /// If the field is of type 'dynamic', we represent it as 'object' because 'dynamic' cannot appear in typeof expressions.
            /// </remarks>
            public ITypeSymbol SafeType => Field.Type.TypeKind == TypeKind.Dynamic
                ? _wellKnownTypes.Object
                : Field.Type;

            /// <summary>
            /// Gets the name of the getter field.
            /// </summary>
            public string GetterFieldName => "getField" + _ordinal;

            /// <summary>
            /// Gets the name of the setter field.
            /// </summary>
            public string SetterFieldName => "setField" + _ordinal;

            public bool HasAccessibleGetter => !IsObsolete && (IsGettableProperty || IsGettableField);

            private bool IsGettableField => IsDeclaredAccessible(Field) && _model.IsAccessible(0, Field);

            /// <summary>
            /// Gets a value indicating whether or not this field represents a property with an accessible, non-obsolete getter. 
            /// </summary>
            private bool IsGettableProperty => Property?.GetMethod != null && _model.IsAccessible(0, Property.GetMethod) && !IsObsolete;

            public bool HasAccessibleSetter => IsSettableProperty || IsSettableField;

            private bool IsSettableField => !Field.IsReadOnly && IsDeclaredAccessible(Field) && _model.IsAccessible(0, Field);

            /// <summary>
            /// Gets a value indicating whether or not this field represents a property with an accessible, non-obsolete setter. 
            /// </summary>
            private bool IsSettableProperty => Property?.SetMethod != null && _model.IsAccessible(0, Property.SetMethod) && !IsObsolete;

            /// <summary>
            /// Gets syntax representing the type of this field.
            /// </summary>
            public TypeSyntax Type => SafeType.ToTypeSyntax();

            /// <summary>
            /// Gets the <see cref="Property"/> which this field is the backing property for, or
            /// <see langword="null" /> if this is not the backing field of an auto-property.
            /// </summary>
            private IPropertySymbol Property
            {
                get
                {
                    if (_property != null)
                    {
                        return _property;
                    }

                    return _property = PropertyUtility.GetMatchingProperty(Field);
                }
            }

            /// <summary>
            /// Gets a value indicating whether or not this field is obsolete.
            /// </summary>
            private bool IsObsolete => Field.HasAttribute(_wellKnownTypes.ObsoleteAttribute) ||
                                       Property != null && Property.HasAttribute(_wellKnownTypes.ObsoleteAttribute);

            /// <summary>
            /// Returns syntax for retrieving the value of this field, deep copying it if necessary.
            /// </summary>
            /// <param name="instance">The instance of the containing type.</param>
            /// <returns>Syntax for retrieving the value of this field.</returns>
            public ExpressionSyntax GetGetter(ExpressionSyntax instance)
            {
                // If the field is the backing field for an accessible auto-property use the property directly.
                ExpressionSyntax result;
                if (IsGettableProperty)
                {
                    result = instance.Member(Property.Name);
                }
                else if (IsGettableField)
                {
                    result = instance.Member(Field.Name);
                }
                else
                {
                    // Retrieve the field using the generated getter.
                    result =
                        InvocationExpression(IdentifierName(GetterFieldName))
                            .AddArgumentListArguments(Argument(instance));
                }

                return result;
            }

            /// <summary>
            /// Returns syntax for setting the value of this field.
            /// </summary>
            /// <param name="instance">The instance of the containing type.</param>
            /// <param name="value">Syntax for the new value.</param>
            /// <returns>Syntax for setting the value of this field.</returns>
            public ExpressionSyntax GetSetter(ExpressionSyntax instance, ExpressionSyntax value)
            {
                // If the field is the backing field for an accessible auto-property use the property directly.
                if (IsSettableProperty)
                {
                    return AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        instance.Member(Property.Name),
                        value);
                }

                if (IsSettableField)
                {
                    return AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        instance.Member(Field.Name),
                        value);
                }

                var instanceArg = Argument(instance);
                if (Field.ContainingType != null && Field.ContainingType.IsValueType)
                {
                    instanceArg = instanceArg.WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword));
                }

                return
                    InvocationExpression(IdentifierName(SetterFieldName))
                        .AddArgumentListArguments(instanceArg, Argument(value));
            }

            private static bool IsDeclaredAccessible(ISymbol symbol) => symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => true,
                _ => false,
            };

            /// <summary>
            /// A comparer for <see cref="SerializableMember"/> which compares by name.
            /// </summary>
            public class Comparer : IComparer<SerializableMember>
            {
                /// <summary>
                /// Gets the singleton instance of this class.
                /// </summary>
                public static Comparer Instance { get; } = new();

                public int Compare(SerializableMember x, SerializableMember y) => string.Compare(x?.Field.Name, y?.Field.Name, StringComparison.Ordinal);
            }
        }
    }
}