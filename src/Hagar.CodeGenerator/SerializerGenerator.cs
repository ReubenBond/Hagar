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
    internal static class SerializerGenerator
    {
        private const string BaseTypeSerializerFieldName = "baseTypeSerializer";
        private const string SerializeMethodName = "Serialize";
        private const string DeserializeMethodName = "Deserialize";

        public static ClassDeclarationSyntax GenerateSerializer(Compilation compilation, LibraryTypes libraryTypes, ISerializableTypeDescription serializableTypeDescription)
        {
            var simpleClassName = GetSimpleClassName(serializableTypeDescription);

            var serializerInterface = serializableTypeDescription.IsValueType ? libraryTypes.ValueSerializer : libraryTypes.PartialSerializer;
            var baseInterface = serializerInterface.ToTypeSyntax(serializableTypeDescription.TypeSyntax);

            var fieldDescriptions = GetFieldDescriptions(serializableTypeDescription, libraryTypes);
            var fields = GetFieldDeclarations(fieldDescriptions);
            var ctor = GenerateConstructor(simpleClassName, fieldDescriptions);

            var serializeMethod = GenerateSerializeMethod(serializableTypeDescription, fieldDescriptions, libraryTypes);
            var deserializeMethod = GenerateDeserializeMethod(serializableTypeDescription, fieldDescriptions, libraryTypes);

            var classDeclaration = ClassDeclaration(simpleClassName)
                .AddBaseListTypes(SimpleBaseType(baseInterface))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fields)
                .AddMembers(ctor, serializeMethod, deserializeMethod);
            if (serializableTypeDescription.IsGenericType)
            {
                classDeclaration = AddGenericTypeConstraints(classDeclaration, serializableTypeDescription);
            }

            return classDeclaration;
        }

        public static string GetSimpleClassName(ISerializableTypeDescription serializableType)
        {
            return $"{CodeGenerator.CodeGeneratorName}_Serializer_{serializableType.Name}";
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

            MemberDeclarationSyntax GetFieldDeclaration(FieldDescription description)
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
            var body = injected.Select(
                f => (StatementSyntax) ExpressionStatement(
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
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("HagarGeneratedCodeHelper"), IdentifierName("UnwrapService")),
                    ArgumentList(SeparatedList(new [] {Argument(ThisExpression()), Argument(expr)})));
            }
        }

        private static List<FieldDescription> GetFieldDescriptions(ISerializableTypeDescription serializableTypeDescription, LibraryTypes libraryTypes)
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
                .Where(t => !libraryTypes.StaticCodecs.Any(c => c.UnderlyingType.Equals(t)))
                .Select(GetCodecDescription));
            return fields;

            CodecFieldDescription GetCodecDescription(ITypeSymbol t)
            {
                var codecType = libraryTypes.FieldCodec.Construct(t).ToTypeSyntax();
                var fieldName = '_' + ToLowerCamelCase(t.GetValidIdentifier()) + "Codec";
                return new CodecFieldDescription(codecType, fieldName, t);
            }

            TypeFieldDescription GetTypeDescription(ITypeSymbol t)
            {
                var fieldName = ToLowerCamelCase(t.GetValidIdentifier()) + "Type";
                return new TypeFieldDescription(libraryTypes.Type.ToTypeSyntax(), fieldName, t);
            }

            string ToLowerCamelCase(string input) => char.IsLower(input, 0) ? input : char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Returns the "expected" type for <paramref name="type"/> which is used for selecting the correct codec.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static ITypeSymbol GetExpectedType(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol)
                return type;
            if (type is IPointerTypeSymbol pointerType)
                throw new NotSupportedException($"Cannot serialize pointer type {pointerType.Name}");
            return type;
        }

        private static MemberDeclarationSyntax GenerateSerializeMethod(
            ISerializableTypeDescription serializableTypeDescription,
            List<FieldDescription> fieldDescriptions,
            LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var writerParam = "writer".ToIdentifierName();
            var instanceParam = "instance".ToIdentifierName();

            var body = new List<StatementSyntax>();
            if (serializableTypeDescription.HasComplexBaseType)
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
            foreach (var member in serializableTypeDescription.Members.OrderBy(m => m.FieldId))
            {
                var fieldIdDelta = member.FieldId - previousFieldId;
                previousFieldId = member.FieldId;

                // Codecs can either be static classes or injected into the constructor.
                // Either way, the member signatures are the same.
                var memberType = GetExpectedType(member.Type);
                var staticCodec = libraryTypes.StaticCodecs.FirstOrDefault(c => c.UnderlyingType.Equals(memberType));
                ExpressionSyntax codecExpression;
                if (staticCodec != null)
                {
                    codecExpression = staticCodec.CodecType.ToNameSyntax();
                }
                else
                {
                    var instanceCodec = fieldDescriptions.OfType<CodecFieldDescription>().First(f => f.UnderlyingType.Equals(memberType));
                    codecExpression = ThisExpression().Member(instanceCodec.FieldName);
                }

                var expectedType = fieldDescriptions.OfType<TypeFieldDescription>().First(f => f.UnderlyingType.Equals(memberType));
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
                                        Argument(instanceParam.Member(member.Name))
                                    })))));
            }

            var parameters = new[]
            {
                Parameter("writer".ToIdentifier()).WithType(libraryTypes.Writer.ToTypeSyntax()).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter("instance".ToIdentifier()).WithType(serializableTypeDescription.TypeSyntax)
            };

            if (serializableTypeDescription.IsValueType)
            {
                parameters[1] = parameters[1].WithModifiers(TokenList(Token(SyntaxKind.InKeyword)));
            }

            return MethodDeclaration(returnType, SerializeMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddTypeParameterListParameters(TypeParameter("TBufferWriter"))
                .AddConstraintClauses(TypeParameterConstraintClause("TBufferWriter").AddConstraints(TypeConstraint(libraryTypes.IBufferWriter.Construct(libraryTypes.Byte).ToTypeSyntax())))
                .AddBodyStatements(body.ToArray());
        }

        private static MemberDeclarationSyntax GenerateDeserializeMethod(
            ISerializableTypeDescription serializableTypeDescription,
            List<FieldDescription> fieldDescriptions,
            LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var readerParam = "reader".ToIdentifierName();
            var instanceParam = "instance".ToIdentifierName();
            var fieldIdVar = "fieldId".ToIdentifierName();
            var headerVar = "header".ToIdentifierName();

            var body = new List<StatementSyntax>
            {
                // C#: uint fieldId = 0;
                LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.UIntKeyword)),
                        SingletonSeparatedList(VariableDeclarator(fieldIdVar.Identifier)
                            .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))))
            };

            if (serializableTypeDescription.HasComplexBaseType)
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

            var parameters = new[]
            {
                Parameter(readerParam.Identifier).WithType(libraryTypes.Reader.ToTypeSyntax()).WithModifiers(TokenList(Token(SyntaxKind.RefKeyword))),
                Parameter(instanceParam.Identifier).WithType(serializableTypeDescription.TypeSyntax)
            };
            
            if (serializableTypeDescription.IsValueType)
            {
                parameters[1] = parameters[1].WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)));
            }

            return MethodDeclaration(returnType, DeserializeMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters)
                .AddBodyStatements(body.ToArray());

            // Create the loop body.
            List<StatementSyntax> GetDeserializerLoopBody()
            {
                return new List<StatementSyntax>
                {
                    // C#: var header = reader.ReadFieldHeader();
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("var"),
                            SingletonSeparatedList(
                                VariableDeclarator(headerVar.Identifier)
                                    .WithInitializer(EqualsValueClause(InvocationExpression(readerParam.Member("ReadFieldHeader"),
                                        ArgumentList())))))),

                    // C#: if (header.IsEndBaseOrEndObject) break;
                    IfStatement(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, headerVar, IdentifierName("IsEndBaseOrEndObject")), BreakStatement()),

                    // C#: fieldId += header.FieldIdDelta;
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.AddAssignmentExpression,
                            fieldIdVar,
                            Token(SyntaxKind.PlusEqualsToken),
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, headerVar, IdentifierName("FieldIdDelta")))),

                    // C#: switch (fieldId) { ... }
                    SwitchStatement(fieldIdVar, List(GetSwitchSections()))
                };
            }

            // Creates switch sections for each member.
            List<SwitchSectionSyntax> GetSwitchSections()
            {
                var switchSections = new List<SwitchSectionSyntax>();
                foreach (var member in serializableTypeDescription.Members)
                {
                    // C#: case <fieldId>:
                    var label = CaseSwitchLabel(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(member.FieldId)));

                    // C#: instance.<member> = this.<codec>.ReadValue(ref reader, header);
                    var codec = fieldDescriptions.OfType<ICodecDescription>()
                        .Concat(libraryTypes.StaticCodecs)
                        .First(f => f.UnderlyingType.Equals(GetExpectedType(member.Type)));
                    
                    // Codecs can either be static classes or injected into the constructor.
                    // Either way, the member signatures are the same.
                    var memberType = GetExpectedType(member.Type);
                    var staticCodec = libraryTypes.StaticCodecs.FirstOrDefault(c => c.UnderlyingType.Equals(memberType));
                    ExpressionSyntax codecExpression;
                    if (staticCodec != null)
                    {
                        codecExpression = staticCodec.CodecType.ToNameSyntax();
                    }
                    else
                    {
                        var instanceCodec = fieldDescriptions.OfType<CodecFieldDescription>().First(f => f.UnderlyingType.Equals(memberType));
                        codecExpression = ThisExpression().Member(instanceCodec.FieldName);
                    }

                    ExpressionSyntax readValueExpression = InvocationExpression(
                        codecExpression.Member("ReadValue"),
                        ArgumentList(SeparatedList(new[] {Argument(readerParam).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)), Argument(headerVar)})));
                    if (!codec.UnderlyingType.Equals(member.Type))
                    {
                        // If the member type type differs from the codec type (eg because the member is an array), cast the result.
                        readValueExpression = CastExpression(member.Type.ToTypeSyntax(), readValueExpression);
                    }

                    var memberAssignment =
                        ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, instanceParam.Member(member.Name), readValueExpression));
                    var caseBody = List(new StatementSyntax[] { memberAssignment, BreakStatement() });

                    // Create the switch section with a break at the end.
                    // C#: break;
                    switchSections.Add(SwitchSection(SingletonList<SwitchLabelSyntax>(label), caseBody));
                }

                // Add the default switch section.
                var consumeUnknown = ExpressionStatement(InvocationExpression(readerParam.Member("ConsumeUnknownField"),
                    ArgumentList(SeparatedList(new[] { Argument(headerVar) }))));
                switchSections.Add(SwitchSection(SingletonList<SwitchLabelSyntax>(DefaultSwitchLabel()), List(new StatementSyntax[] { consumeUnknown, BreakStatement() })));

                return switchSections;
            }
        }

        internal abstract class FieldDescription
        {
            protected FieldDescription(TypeSyntax fieldType, string fieldName)
            {
                this.FieldType = fieldType;
                this.FieldName = fieldName;
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
                this.UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => true;
        }

        internal class TypeFieldDescription : FieldDescription
        {
            public TypeFieldDescription(TypeSyntax fieldType, string fieldName, ITypeSymbol underlyingType) : base(fieldType, fieldName)
            {
                this.UnderlyingType = underlyingType;
            }

            public ITypeSymbol UnderlyingType { get; }
            public override bool IsInjected => false;
        }
    }
        internal interface ICodecDescription
        {
            ITypeSymbol UnderlyingType { get; }
        }

        internal class StaticCodecDescription : ICodecDescription
        {
            public StaticCodecDescription(ITypeSymbol underlyingType, INamedTypeSymbol codecType)
            {
            this.UnderlyingType = underlyingType;
            this.CodecType = codecType;
            }

            public ITypeSymbol UnderlyingType { get; }

            public INamedTypeSymbol CodecType { get; }
        }
}
