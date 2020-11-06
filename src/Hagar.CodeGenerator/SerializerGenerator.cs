using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Hagar.CodeGenerator.InvokableGenerator;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal static class SerializerGenerator
    {
        private const string BaseTypeSerializerFieldName = "baseTypeSerializer";
        private const string SerializeMethodName = "Serialize";
        private const string DeserializeMethodName = "Deserialize";
        private const string WriteFieldMethodName = "WriteField";
        private const string ReadValueMethodName = "ReadValue";

        public static ClassDeclarationSyntax GenerateSerializer(LibraryTypes libraryTypes, ISerializableTypeDescription type)
        {
            var simpleClassName = GetSimpleClassName(type);

            var serializerInterface = type.IsEnumType ? libraryTypes.FieldCodec_1 : type.IsValueType ? libraryTypes.ValueSerializer : libraryTypes.PartialSerializer;
            var baseInterface = serializerInterface.ToTypeSyntax(type.TypeSyntax);

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
            var ctor = GenerateConstructor(simpleClassName, fieldDescriptions);

            var classDeclaration = ClassDeclaration(simpleClassName)
                .AddBaseListTypes(SimpleBaseType(baseInterface))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fieldDeclarations)
                .AddMembers(ctor);

            if (type.IsEnumType)
            {
                var writeMethod = GenerateWriteFieldMethod(type, fieldDescriptions, libraryTypes);
                var readMethod = GenerateReadValueMethod(type, fieldDescriptions, libraryTypes);
                classDeclaration = classDeclaration.AddMembers(writeMethod, readMethod);
            }
            else
            {
                var serializeMethod = GenerateSerializeMethod(type, fieldDescriptions, members, libraryTypes);
                var deserializeMethod = GenerateDeserializeMethod(type, fieldDescriptions, members, libraryTypes);
                classDeclaration = classDeclaration.AddMembers(serializeMethod, deserializeMethod);
            }

            if (type.IsGenericType)
            {
                classDeclaration = AddGenericTypeConstraints(classDeclaration, type);
            }

            return classDeclaration;
        }

        public static string GetSimpleClassName(ISerializableTypeDescription serializableType)
        {
            var uniquifier = RuntimeHelpers.GetHashCode(serializableType).ToString("X");
            return $"{CodeGenerator.CodeGeneratorName}_Serializer_{serializableType.Name}_{uniquifier}";
        }

        private static ClassDeclarationSyntax AddGenericTypeConstraints(ClassDeclarationSyntax classDeclaration, ISerializableTypeDescription serializableType)
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

        private static ConstructorDeclarationSyntax GenerateConstructor(string simpleClassName, List<FieldDescription> fieldDescriptions)
        {
            var injected = fieldDescriptions.Where(f => f.IsInjected).ToList();
            var parameters = injected.Select(f => Parameter(f.FieldName.ToIdentifier()).WithType(f.FieldType));

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
        }

        private static List<FieldDescription> GetFieldDescriptions(
            ISerializableTypeDescription serializableTypeDescription,
            List<ISerializableMember> members,
            LibraryTypes libraryTypes)
        {
            var fields = new List<FieldDescription>();
            fields.AddRange(serializableTypeDescription.Members.Select(m => GetExpectedType(m.Type)).Distinct().Select(GetTypeDescription));

            if (serializableTypeDescription.HasComplexBaseType)
            {
                fields.Add(new InjectedFieldDescription(libraryTypes.PartialSerializer.Construct(serializableTypeDescription.BaseType).ToTypeSyntax(), BaseTypeSerializerFieldName));
            }

            // Add a codec field for any field in the target which does not have a static codec.
            fields.AddRange(serializableTypeDescription.Members
                .Select(m => GetExpectedType(m.Type)).Distinct()
                .Where(t => !libraryTypes.StaticCodecs.Any(c => SymbolEqualityComparer.Default.Equals(c.UnderlyingType, t)))
                .Select(GetCodecDescription));

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
                var codecType = libraryTypes.FieldCodec_1.Construct(t).ToTypeSyntax();
                var fieldName = '_' + ToLowerCamelCase(t.GetValidIdentifier()) + "Codec";
                return new CodecFieldDescription(codecType, fieldName, t);
            }

            TypeFieldDescription GetTypeDescription(ITypeSymbol t)
            {
                var fieldName = ToLowerCamelCase(t.GetValidIdentifier()) + "Type";
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

        private static MemberDeclarationSyntax GenerateWriteFieldMethod(
            ISerializableTypeDescription type,
            List<FieldDescription> serializerFields,
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
            ExpressionSyntax codecExpression;
            if (staticCodec != null)
            {
                codecExpression = staticCodec.CodecType.ToNameSyntax();
            }
            else
            {
                var instanceCodec = serializerFields.OfType<CodecFieldDescription>().First(f => SymbolEqualityComparer.Default.Equals(f.UnderlyingType, type.BaseType));
                codecExpression = ThisExpression().Member(instanceCodec.FieldName);
            }

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
                .AddBodyStatements(body.ToArray());
        }

        private static MemberDeclarationSyntax GenerateReadValueMethod(
            ISerializableTypeDescription type,
            List<FieldDescription> serializerFields,
            LibraryTypes libraryTypes)
        {
            var readerParam = "reader".ToIdentifierName();
            var fieldParam = "field".ToIdentifierName();

            var codecs = serializerFields.OfType<ICodecDescription>()
                    .Concat(libraryTypes.StaticCodecs)
                    .ToList();
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

        internal class InjectedFieldDescription : FieldDescription
        {
            public InjectedFieldDescription(TypeSyntax fieldType, string fieldName) : base(fieldType, fieldName)
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
            public override bool IsInjected => true;
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
                public static Comparer Instance { get; } = new Comparer();

                public int Compare(SerializableMember x, SerializableMember y) => string.Compare(x?.Field.Name, y?.Field.Name, StringComparison.Ordinal);
            }
        }
    }
}