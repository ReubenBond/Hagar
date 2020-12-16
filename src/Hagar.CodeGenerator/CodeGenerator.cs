using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    public class CodeGeneratorOptions
    {
        public string[] GenerateSerializerAttributes { get; set; } = new[] { "System.SerializableAttribute" };
        public List<string> IdAttributeTypes { get; set; } = new List<string> { "Hagar.IdAttribute" };
        public List<string> AliasAttributeTypes { get; set; } = new List<string> { "Hagar.AliasAttribute" };

        public bool GenerateFieldIds { get; set; } = false;
    }

    public class CodeGenerator
    {
        internal const string CodeGeneratorName = "HagarGen";
        private readonly Compilation _compilation;
        private readonly LibraryTypes _libraryTypes;
        private readonly CodeGeneratorOptions _options;
        private readonly INamedTypeSymbol[] _generateSerializerAttributes;

        public CodeGenerator(Compilation compilation, CodeGeneratorOptions options)
        {
            _compilation = compilation;
            _options = options;
            _libraryTypes = LibraryTypes.FromCompilation(compilation, options);
            if (options.GenerateSerializerAttributes != null)
            {
                _generateSerializerAttributes = options.GenerateSerializerAttributes.Select(compilation.GetTypeByMetadataName).ToArray();
            }
        }

        public CompilationUnitSyntax GenerateCode(CancellationToken cancellationToken)
        {
            var namespaceName = "HagarGeneratedCode." + _compilation.AssemblyName;

            // Collect metadata from the compilation.
            var metadataModel = GenerateMetadataModel(cancellationToken);
            var members = new List<MemberDeclarationSyntax>();

            foreach (var type in metadataModel.InvokableInterfaces)
            {
                foreach (var method in type.Methods)
                {
                    var (invokable, generatedInvokerDescription) = InvokableGenerator.Generate(_libraryTypes, type, method);
                    metadataModel.SerializableTypes.Add(generatedInvokerDescription);
                    metadataModel.GeneratedInvokables[method] = generatedInvokerDescription;
                    members.Add(invokable);
                }

                var (proxy, generatedProxyDescription) = ProxyGenerator.Generate(_libraryTypes, type, metadataModel);
                metadataModel.GeneratedProxies.Add(generatedProxyDescription);
                members.Add(proxy);
            }

            // Generate code.
            foreach (var type in metadataModel.SerializableTypes)
            {
                // Generate a partial serializer class for each serializable type.
                members.Add(SerializerGenerator.GenerateSerializer(_libraryTypes, type));

                if (type.IsEmptyConstructable)
                {
                    metadataModel.ActivatableTypes.Add(type);

                    // Generate a partial serializer class for each serializable type.
                    members.Add(ActivatorGenerator.GenerateActivator(_libraryTypes, type));
                }
            }

            // Generate metadata.
            var metadataClass = MetadataGenerator.GenerateMetadata(_compilation, metadataModel, _libraryTypes);
            members.Add(metadataClass);

            var metadataAttribute = AttributeList()
                .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword)))
                .WithAttributes(
                    SingletonSeparatedList(
                        Attribute(_libraryTypes.MetadataProviderAttribute.ToNameSyntax())
                            .AddArgumentListArguments(AttributeArgument(TypeOfExpression(ParseTypeName($"{namespaceName}.{metadataClass.Identifier.Text}"))))));

            return CompilationUnit()
                .WithAttributeLists(List(new[] { metadataAttribute }))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(ParseName(namespaceName))
                        .WithMembers(List(members))
                        .WithUsings(List(new[] { UsingDirective(ParseName("global::Hagar.Codecs")), UsingDirective(ParseName("global::Hagar.GeneratedCodeHelpers")) }))));
        }

        private MetadataModel GenerateMetadataModel(CancellationToken cancellationToken)
        {
            var metadataModel = new MetadataModel();

            foreach (var syntaxTree in _compilation.SyntaxTrees)
            {
                var semanticModel = _compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: false);
                var rootNode = syntaxTree.GetRoot(cancellationToken);
                foreach (var node in GetTypeDeclarations(rootNode))
                {
                    var symbolRaw = semanticModel.GetDeclaredSymbol(node, cancellationToken: cancellationToken);
                    if (symbolRaw is not INamedTypeSymbol symbol)
                    {
                        continue;
                    }

                    bool ShouldGenerateSerializer(INamedTypeSymbol t)
                    {
                        if (!semanticModel.IsAccessible(0, t))
                        {
                            return false;
                        }

                        if (HasAttribute(t, _libraryTypes.GenerateSerializerAttribute, inherited: true) != null)
                        {
                            return true;
                        }

                        if (_generateSerializerAttributes != null)
                        {
                            foreach (var attr in _generateSerializerAttributes)
                            {
                                if (HasAttribute(t, attr, inherited: true) != null)
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }

                    if (GetWellKnownTypeId(symbol) is uint wellKnownTypeId)
                    {
                        metadataModel.WellKnownTypeIds.Add((symbol, wellKnownTypeId));
                    }

                    if (GetTypeAlias(symbol) is string typeAlias)
                    {
                        metadataModel.TypeAliases.Add((symbol, typeAlias));
                    }

                    if (ShouldGenerateSerializer(symbol))
                    {
                        var typeDescription = new SerializableTypeDescription(semanticModel, symbol, GetDataMembers(symbol));
                        metadataModel.SerializableTypes.Add(typeDescription);
                    }

                    if (symbol.TypeKind == TypeKind.Interface)
                    {
                        var attribute = HasAttribute(
                            symbol,
                            _libraryTypes.GenerateMethodSerializersAttribute,
                            inherited: true);
                        if (attribute != null)
                        {
                            var baseClass = (INamedTypeSymbol)attribute.ConstructorArguments[0].Value;
                            var isExtension = (bool)attribute.ConstructorArguments[1].Value;
                            var description = new InvokableInterfaceDescription(
                                _libraryTypes,
                                semanticModel,
                                symbol,
                                GetMethods(symbol),
                                baseClass,
                                isExtension);
                            metadataModel.InvokableInterfaces.Add(description);
                        }
                    }

                    if ((symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Struct) && !symbol.IsAbstract && (symbol.DeclaredAccessibility == Accessibility.Public || symbol.DeclaredAccessibility == Accessibility.Internal))
                    {
                        if (symbol.HasAttribute(_libraryTypes.RegisterSerializerAttribute))
                        {
                            metadataModel.DetectedSerializers.Add(symbol);
                        }

                        if (symbol.HasAttribute(_libraryTypes.RegisterActivatorAttribute))
                        {
                            metadataModel.DetectedActivators.Add(symbol);
                        }
                    }
                }
            }

            return metadataModel;
        }

        private static IEnumerable<MemberDeclarationSyntax> GetTypeDeclarations(SyntaxNode node)
        {
            SyntaxList<MemberDeclarationSyntax> members;
            switch (node)
            {
                case EnumDeclarationSyntax enumDecl:
                    yield return enumDecl;
                    members = new SyntaxList<MemberDeclarationSyntax>();
                    break;
                case TypeDeclarationSyntax type:
                    yield return type;
                    members = type.Members;
                    break;
                case NamespaceDeclarationSyntax ns:
                    members = ns.Members;
                    break;
                case CompilationUnitSyntax compilationUnit:
                    members = compilationUnit.Members;
                    break;
                default:
                    yield break;
            }

            foreach (var member in members)
            {
                foreach (var decl in GetTypeDeclarations(member))
                {
                    yield return decl;
                }
            }
        }

        // Returns descriptions of all data members (fields and properties) 
        private IEnumerable<IMemberDescription> GetDataMembers(INamedTypeSymbol symbol)
        {
            var hasAttributes = false;
            foreach (var member in symbol.GetMembers())
            {
                if (member.IsStatic)
                {
                    continue;
                }

                if (member.HasAttribute(_libraryTypes.NonSerializedAttribute))
                {
                    continue;
                }

                if (_libraryTypes.IdAttributeTypes.Any(t => member.HasAttribute(t)))
                {
                    hasAttributes = true;
                    break;
                }
            }

            var nextFieldId = (ushort)0;
            foreach (var member in symbol.GetMembers().OrderBy(m => m.MetadataName))
            {
                // Only consider fields and properties.
                if (!(member is IFieldSymbol || member is IPropertySymbol))
                {
                    continue;
                }

                if (member.HasAttribute(_libraryTypes.NonSerializedAttribute))
                {
                    continue;
                }

                ushort? GetId(ISymbol memberSymbol)
                {
                    var idAttr = memberSymbol.GetAttributes().FirstOrDefault(attr => _libraryTypes.IdAttributeTypes.Any(t => SymbolEqualityComparer.Default.Equals(t, attr.AttributeClass)));
                    if (idAttr is null)
                    {
                        return null;
                    }

                    var id = (ushort)idAttr.ConstructorArguments.First().Value;
                    return id;
                }

                if (member is IPropertySymbol prop)
                {
                    var id = GetId(prop);
                    if (!id.HasValue)
                    {
                        if (hasAttributes)
                        {
                            continue;
                        }

                        id = ++nextFieldId;
                    }

                    yield return new PropertyDescription(id.Value, prop);
                }

                if (member is IFieldSymbol field)
                {
                    var id = GetId(field);
                    if (!id.HasValue)
                    {
                        prop = PropertyUtility.GetMatchingProperty(field);

                        if (prop is null)
                        {
                            continue;
                        }

                        if (prop.HasAttribute(_libraryTypes.NonSerializedAttribute))
                        {
                            continue;
                        }

                        id = GetId(prop);
                    }

                    if (!id.HasValue)
                    {
                        if (hasAttributes || !_options.GenerateFieldIds)
                        {
                            continue;
                        }

                        id = nextFieldId++;
                    }

                    yield return new FieldDescription(id.Value, field);
                }
            }
        }

        private uint? GetWellKnownTypeId(INamedTypeSymbol typeSymbol)
        {
            var attr = typeSymbol.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(_libraryTypes.WellKnownIdAttribute, attr.AttributeClass));
            if (attr is null)
            {
                return null;
            }

            var id = (uint)attr.ConstructorArguments.First().Value;
            return id;
        }

        private string GetTypeAlias(INamedTypeSymbol typeSymbol)
        {
            var attr = typeSymbol.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(_libraryTypes.WellKnownAliasAttribute, attr.AttributeClass));
            if (attr is null)
            {
                return null;
            }

            var value = (string)attr.ConstructorArguments.First().Value;
            return value;
        }

        // Returns descriptions of all methods 
        private static IEnumerable<MethodDescription> GetMethods(INamedTypeSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                if (member is IMethodSymbol method)
                {
                    yield return new MethodDescription(method);
                }
            }
        }

        // Returns true if the type declaration has the specified attribute.
        private static AttributeData HasAttribute(INamedTypeSymbol symbol, ISymbol attributeType, bool inherited = false)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                {
                    return attribute;
                }
            }

            if (inherited)
            {
                foreach (var iface in symbol.AllInterfaces)
                {
                    foreach (var attribute in iface.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                        {
                            return attribute;
                        }
                    }
                }

                while ((symbol = symbol.BaseType) != null)
                {
                    foreach (var attribute in symbol.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                        {
                            return attribute;
                        }
                    }
                }
            }

            return null;
        }

        internal static AttributeSyntax GetGeneratedCodeAttributeSyntax()
        {
            var version = typeof(CodeGenerator).Assembly.GetName().Version.ToString();
            return
                Attribute(ParseName("System.CodeDom.Compiler.GeneratedCodeAttribute"))
                    .AddArgumentListArguments(
                        AttributeArgument(CodeGeneratorName.GetLiteralExpression()),
                        AttributeArgument(version.GetLiteralExpression()));
        }
    }
}