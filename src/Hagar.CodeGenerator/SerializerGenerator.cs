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
    internal static class SerializerGenerator
    {
        private const string BaseTypeSerializerFieldName = "_baseTypeSerializer";
        private const string ActivatorFieldName = "_activator";
        private const string SerializeMethodName = "Serialize";
        private const string DeserializeMethodName = "Deserialize";
        private const string WriteFieldMethodName = "WriteField";
        private const string ReadValueMethodName = "ReadValue";
        private const string CodecFieldTypeFieldName = "_codecFieldType";

        public static ClassDeclarationSyntax GenerateSerializer(LibraryTypes libraryTypes, ISerializableTypeDescription type, Dictionary<string, List<MemberDeclarationSyntax>> partialTypeSerializers)
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

            var classDeclaration = ClassDeclaration(simpleClassName)
                .AddBaseListTypes(SimpleBaseType(libraryTypes.FieldCodec_1.ToTypeSyntax(type.TypeSyntax)))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fieldDeclarations)
                .AddMembers(ctor);

            if (type.IsEnumType)
            {
                var writeMethod = GenerateEnumWriteMethod(type, libraryTypes);
                var readMethod = GenerateEnumReadMethod(type, libraryTypes);
                classDeclaration = classDeclaration.AddMembers(writeMethod, readMethod);
            }
            else
            {
                var serializeMethod = GenerateSerializeMethod(type, fieldDescriptions, members, libraryTypes);
                var deserializeMethod = GenerateDeserializeMethod(type, fieldDescriptions, members, libraryTypes);
                var writeFieldMethod = GenerateCompoundTypeWriteFieldMethod(type, libraryTypes);
                var readValueMethod = GenerateCompoundTypeReadValueMethod(type, fieldDescriptions, libraryTypes);
                classDeclaration = classDeclaration.AddMembers(serializeMethod, deserializeMethod, writeFieldMethod, readValueMethod);

                var serializerInterface = type.IsValueType ? libraryTypes.ValueSerializer : libraryTypes.PartialSerializer;
                classDeclaration = classDeclaration.AddBaseListTypes(SimpleBaseType(serializerInterface.ToTypeSyntax(type.TypeSyntax)));
            }

            if (type.IsGenericType)
            {
                classDeclaration = AddGenericTypeParameters(classDeclaration, type);
            }

            if (type.IsPartial)
            {
                if (!partialTypeSerializers.TryGetValue(type.Namespace, out var nsMembers))
                {
                    nsMembers = partialTypeSerializers[type.Namespace] = new List<MemberDeclarationSyntax>();
                }

                // Generate deserialization constructor
                // Generate serialization method <TBufferWriter>
                // Generate partial type declaration (class, struct, record)
            }

            if (type.IsPartial)
            {
                if (!partialTypeSerializers.TryGetValue(type.Namespace, out var nsMembers))
                {
                    nsMembers = partialTypeSerializers[type.Namespace] = new List<MemberDeclarationSyntax>();
                }

                // Generate deserialization constructor
                // Generate serialization method <TBufferWriter>
                // Generate partial type declaration (class, struct, record)
            }

            return classDeclaration;
        }

        public static string GetSimpleClassName(ISerializableTypeDescription serializableType) => GetSimpleClassName(serializableType.Namespace, serializableType.Name);

        public static string GetSimpleClassName(string namespaceName, string name) => $"{CodeGenerator.CodeGeneratorName}_{namespaceName.Replace('.', '_')}_Codec_{name}";

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
                    case CodecFieldTypeFieldDescription type:
                        return FieldDeclaration(
                                VariableDeclaration(
                                    type.FieldType,
                                    SingletonSeparatedList(VariableDeclarator(type.FieldName)
                                        .WithInitializer(EqualsValueClause(TypeOfExpression(type.CodecFieldType))))))
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
                        case CodecFieldDescription codec when !field.IsInjected:
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

            fields.Add(new CodecFieldTypeFieldDescription(libraryTypes.Type.ToTypeSyntax(), CodecFieldTypeFieldName, serializableTypeDescription.TypeSyntax)); 

            if (serializableTypeDescription.HasComplexBaseType)
            {
                fields.Add(new PartialSerializerFieldDescription(libraryTypes.PartialSerializer.Construct(serializableTypeDescription.BaseType).ToTypeSyntax(), BaseTypeSerializerFieldName));
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
                .Where(t => !libraryTypes.StaticCodecs.Any(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, t)))
                .Select(type => GetCodecDescription(type)));

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

            CodecFieldDescription GetCodecDescription(ITypeSymbol t)
            {
                TypeSyntax codecType;
                if (t.HasAttribute(libraryTypes.GenerateSerializerAttribute))
                {
                    // Use the concrete generated type and avoid expensive interface dispatch
                    if (t is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
                    {
                        // Construct the full generic type name
                        var ns = QualifiedName(IdentifierName("HagarGeneratedCode"), IdentifierName(t.ContainingAssembly.Name));
                        var name = GenericName(Identifier(GetSimpleClassName(t.ContainingNamespace.Name, t.Name)), TypeArgumentList(SeparatedList(namedTypeSymbol.TypeArguments.Select(arg => arg.ToTypeSyntax()))));
                        codecType = QualifiedName(ns, name);
                    }
                    else
                    {
                        var simpleName = $"HagarGeneratedCode.{t.ContainingAssembly.Name}.{GetSimpleClassName(t.ContainingNamespace.Name, t.Name)}";
                        codecType = ParseTypeName(simpleName);
                    }
                }
                else if (libraryTypes.WellKnownCodecs.FirstOrDefault(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, t)) is WellKnownCodecDescription codec)
                {
                    // The codec is not a static codec and is also not a generic codec.
                    codecType = codec.CodecType.ToTypeSyntax();
                }
                else if (t is INamedTypeSymbol named && libraryTypes.WellKnownCodecs.FirstOrDefault(c => t is INamedTypeSymbol named && named.ConstructedFrom is ISymbol unboundFieldType && SymbolEqualityComparer.Default.Equals(c.UnderlyingType, unboundFieldType)) is WellKnownCodecDescription genericCodec)
                {
                    // Construct the generic codec type using the field's type arguments.
                    codecType = genericCodec.CodecType.Construct(named.TypeArguments.ToArray()).ToTypeSyntax();
                }
                else
                {
                    // Use the IFieldCodec<TField> interface
                    codecType = libraryTypes.FieldCodec_1.Construct(t).ToTypeSyntax();
                }

                var fieldName = '_' + ToLowerCamelCase(t.GetValidIdentifier()) + "Codec";
                return new CodecFieldDescription(codecType, fieldName, t);
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
        /// Returns the "expected" type for <paramref name="type"/> which is used for selecting the correct codec.
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

        private static MemberDeclarationSyntax GenerateSerializeMethod(
            ISerializableTypeDescription type,
            List<FieldDescription> serializerFields,
            List<ISerializableMember> members,
            LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var writerParam = "writer".ToIdentifierName();
            var instanceParam = "instance".ToIdentifierName();

            var body = new List<StatementSyntax>();
            if (type.HasComplexBaseType)
            {
                body.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            ThisExpression().Member(BaseTypeSerializerFieldName.ToIdentifierName()).Member(SerializeMethodName),
                            ArgumentList(SeparatedList(new[] { Argument(writerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)), Argument(instanceParam) })))));
                body.Add(ExpressionStatement(InvocationExpression(writerParam.Member("WriteEndBase"), ArgumentList())));
            }

            // Order members according to their FieldId, since fields must be serialized in order and FieldIds are serialized as deltas.
            uint previousFieldId = 0;
            foreach (var member in members.OrderBy(m => m.Description.FieldId))
            {
                var description = member.Description;
                var fieldIdDelta = description.FieldId - previousFieldId;
                previousFieldId = description.FieldId;

                // Codecs can either be static classes or injected into the constructor.
                // Either way, the member signatures are the same.
                var memberType = GetExpectedType(description.Type);
                var staticCodec = libraryTypes.StaticCodecs.FirstOrDefault(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, memberType));
                ExpressionSyntax codecExpression;
                if (staticCodec != null)
                {
                    codecExpression = staticCodec.CodecType.ToNameSyntax();
                }
                else
                {
                    var instanceCodec = serializerFields.OfType<CodecFieldDescription>().First(f => SymbolEqualityComparer.Default.Equals(f.UnderlyingType, memberType));
                    codecExpression = ThisExpression().Member(instanceCodec.FieldName);
                }

                var expectedType = serializerFields.OfType<TypeFieldDescription>().First(f => SymbolEqualityComparer.Default.Equals(f.UnderlyingType, memberType));
                body.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            codecExpression.Member("WriteField"),
                            ArgumentList(
                                SeparatedList(
                                    new[]
                                    {
                                        Argument(writerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(fieldIdDelta))),
                                        Argument(expectedType.FieldName.ToIdentifierName()),
                                        Argument(member.GetGetter(instanceParam))
                                    })))));
            }

            var parameters = new[]
            {
                Parameter("writer".ToIdentifier()).WithType(libraryTypes.Writer.ToTypeSyntax()).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter("instance".ToIdentifier()).WithType(type.TypeSyntax)
            };

            if (type.IsValueType)
            {
                parameters[1] = parameters[1].WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));
            }

            return MethodDeclaration(returnType, SerializeMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddTypeParameterListParameters(TypeParameter("TBufferWriter"))
                .AddConstraintClauses(TypeParameterConstraintClause("TBufferWriter").AddConstraints(TypeConstraint(libraryTypes.IBufferWriter.Construct(libraryTypes.Byte).ToTypeSyntax())))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetMethodImplAttributeSyntax())))
                .AddBodyStatements(body.ToArray());
        }

        private static MemberDeclarationSyntax GenerateDeserializeMethod(
            ISerializableTypeDescription type,
            List<FieldDescription> serializerFields,
            List<ISerializableMember> members,
            LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var readerParam = "reader".ToIdentifierName();
            var instanceParam = "instance".ToIdentifierName();
            var idVar = "id".ToIdentifierName();
            var headerVar = "header".ToIdentifierName();
            var readHeaderLocalFunc = "ReadHeader".ToIdentifierName();
            var readHeaderEndLocalFunc = "ReadHeaderExpectingEndBaseOrEndObject".ToIdentifierName();

            var body = new List<StatementSyntax>
            {
                // C#: int id = 0;
                LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(VariableDeclarator(idVar.Identifier)
                            .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))))),

                // C#: Field header = default;
                LocalDeclarationStatement(
                    VariableDeclaration(
                        libraryTypes.Field.ToTypeSyntax(),
                        SingletonSeparatedList(VariableDeclarator(headerVar.Identifier)
                            .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))))
            };

            if (type.HasComplexBaseType)
            {
                // C#: this.baseTypeSerializer.Deserialize(ref reader, instance);
                body.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            ThisExpression().Member(BaseTypeSerializerFieldName.ToIdentifierName()).Member(DeserializeMethodName),
                            ArgumentList(SeparatedList(new[]
                            {
                                Argument(readerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                Argument(instanceParam)
                            })))));
            }

            body.Add(WhileStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression), Block(GetDeserializerLoopBody())));

            var genericParam = ParseTypeName("TInput");
            var parameters = new[]
            {
                Parameter(readerParam.Identifier).WithType(libraryTypes.Reader.ToTypeSyntax(genericParam)).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter(instanceParam.Identifier).WithType(type.TypeSyntax)
            };

            if (type.IsValueType)
            {
                parameters[1] = parameters[1].WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));
            }

            return MethodDeclaration(returnType, DeserializeMethodName)
                .AddTypeParameterListParameters(TypeParameter("TInput"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetMethodImplAttributeSyntax())))
                .AddBodyStatements(body.ToArray());

            // Create the loop body.
            List<StatementSyntax> GetDeserializerLoopBody()
            {
                var loopBody = new List<StatementSyntax>();
                var codecs = serializerFields.OfType<ICodecDescription>()
                        .Concat(libraryTypes.StaticCodecs)
                        .ToList();

                var orderedMembers = members.OrderBy(m => m.Description.FieldId).ToList();
                var lastMember = orderedMembers.LastOrDefault();

                // C#: id = HagarGeneratedCodeHelper.ReadHeader(ref reader, ref header, id);
                {
                    var readHeaderMethodName = orderedMembers.Count == 0 ? "ReadHeaderExpectingEndBaseOrEndObject" : "ReadHeader";
                    var readFieldHeader =
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(idVar.Identifier),
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("HagarGeneratedCodeHelper"), IdentifierName(readHeaderMethodName)),
                                    ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(readerParam).WithRefKindKeyword(Token(SyntaxKind.RefKeyword)),
                                        Argument(headerVar).WithRefKindKeyword(Token(SyntaxKind.RefKeyword)),
                                        Argument(idVar)
                                    })))));
                    loopBody.Add(readFieldHeader);
                }

                foreach (var member in orderedMembers)
                {
                    var description = member.Description;

                    // C#: instance.<member> = <codec>.ReadValue(ref reader, header);
                    // Codecs can either be static classes or injected into the constructor.
                    // Either way, the member signatures are the same.
                    var codec = codecs.First(f => SymbolEqualityComparer.Default.Equals(f.UnderlyingType, GetExpectedType(description.Type)));
                    var memberType = GetExpectedType(description.Type);
                    var staticCodec = libraryTypes.StaticCodecs.FirstOrDefault(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, memberType));
                    ExpressionSyntax codecExpression;
                    if (staticCodec != null)
                    {
                        codecExpression = staticCodec.CodecType.ToNameSyntax();
                    }
                    else
                    {
                        var instanceCodec = serializerFields.OfType<CodecFieldDescription>().First(f => SymbolEqualityComparer.Default.Equals(f.UnderlyingType, memberType));
                        codecExpression = ThisExpression().Member(instanceCodec.FieldName);
                    }

                    ExpressionSyntax readValueExpression = InvocationExpression(
                        codecExpression.Member("ReadValue"),
                        ArgumentList(SeparatedList(new[] { Argument(readerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)), Argument(headerVar) })));
                    if (!codec.UnderlyingType.Equals(member.Type))
                    {
                        // If the member type type differs from the codec type (eg because the member is an array), cast the result.
                        readValueExpression = CastExpression(description.Type.ToTypeSyntax(), readValueExpression);
                    }

                    var memberAssignment = ExpressionStatement(member.GetSetter(instanceParam, readValueExpression));

                    var readHeaderMethodName = ReferenceEquals(member, lastMember) ? "ReadHeaderExpectingEndBaseOrEndObject" : "ReadHeader";
                    var readFieldHeader =
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(idVar.Identifier),
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("HagarGeneratedCodeHelper"), IdentifierName(readHeaderMethodName)),
                                    ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(readerParam).WithRefKindKeyword(Token(SyntaxKind.RefKeyword)),
                                        Argument(headerVar).WithRefKindKeyword(Token(SyntaxKind.RefKeyword)),
                                        Argument(idVar)
                                    })))));

                    var ifBody = Block(List(new StatementSyntax[] { memberAssignment, readFieldHeader }));
                    
                    // C#: if (id == <fieldId>) { ... }
                    var ifStatement = IfStatement(BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(idVar.Identifier), LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal((int)description.FieldId))),
                        ifBody);

                    loopBody.Add(ifStatement);
                }

                // C#: if (id == -1) { break; }
                loopBody.Add(IfStatement(BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(idVar.Identifier), LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(-1))),
                    Block(List(new StatementSyntax[] { BreakStatement() }))));

                // Consume any unknown fields
                // C#: reader.ConsumeUnknownField(header);
                var consumeUnknown = ExpressionStatement(InvocationExpression(readerParam.Member("ConsumeUnknownField"),
                    ArgumentList(SeparatedList(new[] { Argument(headerVar) }))));
                loopBody.Add(consumeUnknown);

                return loopBody;
            }
        }

        private static MemberDeclarationSyntax GenerateCompoundTypeWriteFieldMethod(
            ISerializableTypeDescription type,
            LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var writerParam = "writer".ToIdentifierName();
            var fieldIdDeltaParam = "fieldIdDelta".ToIdentifierName();
            var expectedTypeParam = "expectedType".ToIdentifierName();
            var valueParam = "value".ToIdentifierName();
            var valueTypeField = "valueType".ToIdentifierName();

            var innerBody = new List<StatementSyntax>();

            if (!type.IsValueType)
            {
                if (type.TrackReferences)
                {
                    // C#: if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value)) { return; }
                    innerBody.Add(
                        IfStatement(
                            InvocationExpression(
                                IdentifierName("ReferenceCodec").Member("TryWriteReferenceField"),
                                ArgumentList(SeparatedList(new[]
                                {
                            Argument(writerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                            Argument(fieldIdDeltaParam),
                            Argument(expectedTypeParam),
                            Argument(valueParam)
                                }))),
                            Block(ReturnStatement()))
                    );
                }
                else
                {
                    // C#: if (value is null) { ReferenceCodec.WriteNullReference(ref writer, fieldIdDelta, expectedType); return; }
                    innerBody.Add(
                        IfStatement(
                            IsPatternExpression(valueParam, ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                            Block(
                                ExpressionStatement(InvocationExpression(IdentifierName("ReferenceCodec").Member("WriteNullReference"),
                                    ArgumentList(SeparatedList(new[]
                                    {
                                        Argument(writerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                        Argument(fieldIdDeltaParam),
                                        Argument(expectedTypeParam)
                                    })))),
                                ReturnStatement()))
                    );
                }
            }
            else
            {
                // C#: ReferenceCodec.MarkValueField(reader.Session);
                innerBody.Add(ExpressionStatement(InvocationExpression(IdentifierName("ReferenceCodec").Member("MarkValueField"), ArgumentList(SingletonSeparatedList(Argument(writerParam.Member("Session")))))));
            }

            // Generate the most appropriate expression to get the field type.
            ExpressionSyntax valueTypeInitializer = type.IsValueType switch {
                true => IdentifierName(CodecFieldTypeFieldName),
                false => ConditionalAccessExpression(valueParam, InvocationExpression(MemberBindingExpression(IdentifierName("GetType"))))
            };

            ExpressionSyntax valueTypeExpression = type.IsSealedType switch
            {
                true => IdentifierName(CodecFieldTypeFieldName),
                false => valueTypeField
            };

            // C#: writer.WriteStartObject(fieldIdDelta, expectedType, fieldType);
            innerBody.Add(
                ExpressionStatement(InvocationExpression(writerParam.Member("WriteStartObject"),
                ArgumentList(SeparatedList(new[]{
                            Argument(fieldIdDeltaParam),
                            Argument(expectedTypeParam),
                            Argument(valueTypeExpression)
                    })))
                ));

            // C#: this.Serialize(ref writer, [ref] value);
            var valueParamArgument = type.IsValueType switch
            {
                true => Argument(valueParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                false => Argument(valueParam)
            };

            innerBody.Add(
                ExpressionStatement(
                    InvocationExpression(
                        ThisExpression().Member(SerializeMethodName),
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    Argument(writerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                    valueParamArgument
                                })))));

            // C#: writer.WriteEndObject();
            innerBody.Add(ExpressionStatement(InvocationExpression(writerParam.Member("WriteEndObject"))));

            List<StatementSyntax> body;
            if (type.IsSealedType)
            {
                body = innerBody;
            }
            else
            {
                // For types which are not sealed/value types, add some extra logic to support sub-types:
                body = new List<StatementSyntax>
                {
                    // C#: var fieldType = value?.GetType();
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            libraryTypes.Type.ToTypeSyntax(),
                            SingletonSeparatedList(VariableDeclarator(valueTypeField.Identifier)
                                .WithInitializer(EqualsValueClause(valueTypeInitializer))))),
                        
                    // C#: if (fieldType is null || fieldType == typeof(TField)) { <inner body> }
                    // C#: else { HagarGeneratedCodeHelper.SerializeUnexpectedType(ref writer, fieldIdDelta, expectedType, value); }
                    IfStatement(
                        BinaryExpression(SyntaxKind.LogicalOrExpression,
                        IsPatternExpression(valueTypeField, ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                        BinaryExpression(SyntaxKind.EqualsExpression, valueTypeField, IdentifierName(CodecFieldTypeFieldName))),
                        Block(innerBody),
                        ElseClause(Block(new StatementSyntax[]
                        {
                            ExpressionStatement(
                                InvocationExpression(
                                    IdentifierName("HagarGeneratedCodeHelper").Member("SerializeUnexpectedType"),
                                    ArgumentList(
                                        SeparatedList(new [] {
                                            Argument(writerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                            Argument(fieldIdDeltaParam),
                                            Argument(expectedTypeParam),
                                            Argument(valueParam)
                                        }))))
                        })))
                };
            }

            var parameters = new[]
            {
                Parameter("writer".ToIdentifier()).WithType(libraryTypes.Writer.ToTypeSyntax()).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter("fieldIdDelta".ToIdentifier()).WithType(libraryTypes.UInt32.ToTypeSyntax()),
                Parameter("expectedType".ToIdentifier()).WithType(libraryTypes.Type.ToTypeSyntax()),
                Parameter("value".ToIdentifier()).WithType(type.TypeSyntax)
            };

            return MethodDeclaration(returnType, WriteFieldMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddTypeParameterListParameters(TypeParameter("TBufferWriter"))
                .AddConstraintClauses(TypeParameterConstraintClause("TBufferWriter").AddConstraints(TypeConstraint(libraryTypes.IBufferWriter.Construct(libraryTypes.Byte).ToTypeSyntax())))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetMethodImplAttributeSyntax())))
                .AddBodyStatements(body.ToArray());
        }

        private static MemberDeclarationSyntax GenerateCompoundTypeReadValueMethod(
            ISerializableTypeDescription type,
            List<FieldDescription> serializerFields,
            LibraryTypes libraryTypes)
        {
            var readerParam = "reader".ToIdentifierName();
            var fieldParam = "field".ToIdentifierName();
            var resultVar = "result".ToIdentifierName();
            var readerInputTypeParam = ParseTypeName("TInput");

            var body = new List<StatementSyntax>();
            var innerBody = new List<StatementSyntax>();

            if (!type.IsValueType)
            {
                // C#: if (field.WireType == WireType.Reference) { return ReferenceCodec.ReadReference<TField, TInput>(ref reader, field); }
                body.Add(
                    IfStatement(
                        BinaryExpression(SyntaxKind.EqualsExpression, fieldParam.Member("WireType"), libraryTypes.WireType.ToTypeSyntax().Member("Reference")),
                        Block(ReturnStatement(InvocationExpression(
                            IdentifierName("ReferenceCodec").Member("ReadReference", new[] { type.TypeSyntax, readerInputTypeParam }),
                            ArgumentList(SeparatedList(new[]
                            {
                                Argument(readerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                Argument(fieldParam),
                            }))))))
                    );
            }

            ExpressionSyntax createValueExpression = type.UseActivator switch
            {
                true => InvocationExpression(serializerFields.OfType<ActivatorFieldDescription>().Single().FieldName.ToIdentifierName().Member("Create")),
                false => type.GetObjectCreationExpression(libraryTypes)
            };

            // C#: TField result = _activator.Create();
            // or C#: TField result = new TField();
            innerBody.Add(LocalDeclarationStatement(
                VariableDeclaration(
                    type.TypeSyntax,
                    SingletonSeparatedList(VariableDeclarator(resultVar.Identifier)
                    .WithInitializer(EqualsValueClause(createValueExpression))))));

            if (type.TrackReferences)
            {
                // C#: ReferenceCodec.RecordObject(reader.Session, result);
                innerBody.Add(ExpressionStatement(InvocationExpression(IdentifierName("ReferenceCodec").Member("RecordObject"), ArgumentList(SeparatedList(new[] { Argument(readerParam.Member("Session")), Argument(resultVar) })))));
            }
            else
            {
                // C#: ReferenceCodec.MarkValueField(reader.Session);
                innerBody.Add(ExpressionStatement(InvocationExpression(IdentifierName("ReferenceCodec").Member("MarkValueField"), ArgumentList(SingletonSeparatedList(Argument(readerParam.Member("Session")))))));
            }

            // C#: this.Deserializer(ref reader, [ref] result);
            var resultArgument = type.IsValueType switch
            {
                true => Argument(resultVar).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                false => Argument(resultVar)
            };
            innerBody.Add(
                ExpressionStatement(
                    InvocationExpression(
                        ThisExpression().Member(DeserializeMethodName),
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    Argument(readerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                    resultArgument
                                })))));

            innerBody.Add(ReturnStatement(resultVar));

            if (type.IsSealedType)
            {
                body.AddRange(innerBody);
            }
            else
            {
                // C#: var fieldType = field.FieldType;
                var valueTypeField = "valueType".ToIdentifierName();
                body.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            libraryTypes.Type.ToTypeSyntax(),
                            SingletonSeparatedList(VariableDeclarator(valueTypeField.Identifier)
                                .WithInitializer(EqualsValueClause(fieldParam.Member("FieldType")))))));
                body.Add(
                    IfStatement(
                        BinaryExpression(SyntaxKind.LogicalOrExpression,
                        IsPatternExpression(valueTypeField, ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))),
                        BinaryExpression(SyntaxKind.EqualsExpression, valueTypeField, IdentifierName(CodecFieldTypeFieldName))),
                        Block(innerBody)));

                body.Add(ReturnStatement(
                                InvocationExpression(
                                    IdentifierName("HagarGeneratedCodeHelper").Member("DeserializeUnexpectedType", new[] { readerInputTypeParam, type.TypeSyntax }),
                                    ArgumentList(
                                        SeparatedList(new[] {
                                            Argument(readerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                            Argument(fieldParam)
                                        })))));
            }
            
            var parameters = new[]
            {
                Parameter(readerParam.Identifier).WithType(libraryTypes.Reader.ToTypeSyntax(readerInputTypeParam)).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter(fieldParam.Identifier).WithType(libraryTypes.Field.ToTypeSyntax())
            };

            return MethodDeclaration(type.TypeSyntax, ReadValueMethodName)
                .AddTypeParameterListParameters(TypeParameter("TInput"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetMethodImplAttributeSyntax())))
                .AddBodyStatements(body.ToArray());
        }


        private static MemberDeclarationSyntax GenerateEnumWriteMethod(
            ISerializableTypeDescription type,
            LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var writerParam = "writer".ToIdentifierName();
            var fieldIdDeltaParam = "fieldIdDelta".ToIdentifierName();
            var expectedTypeParam = "expectedType".ToIdentifierName();
            var valueParam = "value".ToIdentifierName();

            var body = new List<StatementSyntax>();

            // Codecs can either be static classes or injected into the constructor.
            // Either way, the member signatures are the same.
            var staticCodec = libraryTypes.StaticCodecs.FirstOrDefault(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, type.BaseType));
            var codecExpression = staticCodec.CodecType.ToNameSyntax();

            body.Add(
                ExpressionStatement(
                    InvocationExpression(
                        codecExpression.Member("WriteField"),
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    Argument(writerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
                                    Argument(fieldIdDeltaParam),
                                    Argument(expectedTypeParam),
                                    Argument(CastExpression(type.BaseType.ToTypeSyntax(), valueParam))
                                })))));

            var parameters = new[]
            {
                Parameter("writer".ToIdentifier()).WithType(libraryTypes.Writer.ToTypeSyntax()).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter("fieldIdDelta".ToIdentifier()).WithType(libraryTypes.UInt32.ToTypeSyntax()),
                Parameter("expectedType".ToIdentifier()).WithType(libraryTypes.Type.ToTypeSyntax()),
                Parameter("value".ToIdentifier()).WithType(type.TypeSyntax)
            };

            return MethodDeclaration(returnType, WriteFieldMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddTypeParameterListParameters(TypeParameter("TBufferWriter"))
                .AddConstraintClauses(TypeParameterConstraintClause("TBufferWriter").AddConstraints(TypeConstraint(libraryTypes.IBufferWriter.Construct(libraryTypes.Byte).ToTypeSyntax())))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetMethodImplAttributeSyntax())))
                .AddBodyStatements(body.ToArray());
        }

        private static MemberDeclarationSyntax GenerateEnumReadMethod(
            ISerializableTypeDescription type,
            LibraryTypes libraryTypes)
        {
            var readerParam = "reader".ToIdentifierName();
            var fieldParam = "field".ToIdentifierName();

            var staticCodec = libraryTypes.StaticCodecs.FirstOrDefault(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, type.BaseType));
            ExpressionSyntax codecExpression = staticCodec.CodecType.ToNameSyntax();
            ExpressionSyntax readValueExpression = InvocationExpression(
                codecExpression.Member("ReadValue"),
                ArgumentList(SeparatedList(new[] { Argument(readerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)), Argument(fieldParam) })));

            readValueExpression = CastExpression(type.TypeSyntax, readValueExpression);
            var body = new List<StatementSyntax>
            {
                ReturnStatement(readValueExpression)
            };

            var genericParam = ParseTypeName("TInput");
            var parameters = new[]
            {
                Parameter(readerParam.Identifier).WithType(libraryTypes.Reader.ToTypeSyntax(genericParam)).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter(fieldParam.Identifier).WithType(libraryTypes.Field.ToTypeSyntax())
            };

            return MethodDeclaration(type.TypeSyntax, ReadValueMethodName)
                .AddTypeParameterListParameters(TypeParameter("TInput"))
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

        internal class PartialSerializerFieldDescription : FieldDescription
        {
            public PartialSerializerFieldDescription(TypeSyntax fieldType, string fieldName) : base(fieldType, fieldName)
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

        internal class CodecFieldDescription : FieldDescription, ICodecDescription
        {
            public CodecFieldDescription(TypeSyntax fieldType, string fieldName, ITypeSymbol underlyingType) : base(fieldType, fieldName)
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

        internal class CodecFieldTypeFieldDescription : FieldDescription
        {
            public CodecFieldTypeFieldDescription(TypeSyntax fieldType, string fieldName, TypeSyntax codecFieldType) : base(fieldType, fieldName)
            {
                CodecFieldType = codecFieldType;
            }

            public TypeSyntax CodecFieldType { get; }
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
                public static Comparer Instance { get; } = new Comparer();

                public int Compare(SerializableMember x, SerializableMember y) => string.Compare(x?.Field.Name, y?.Field.Name, StringComparison.Ordinal);
            }
        }
    }
}