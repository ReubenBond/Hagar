using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
            GeneratedNamespaceName = "HagarGeneratedCode." + compilation.AssemblyName;
            if (options.GenerateSerializerAttributes != null)
            {
                _generateSerializerAttributes = options.GenerateSerializerAttributes.Select(compilation.GetTypeByMetadataName).ToArray();
            }
        }

        public string GeneratedNamespaceName { get; }

        public CompilationUnitSyntax GenerateCode(CancellationToken cancellationToken)
        {
            var partialTypeSerializers = new Dictionary<string, List<MemberDeclarationSyntax>>();

            // Collect metadata from the compilation.
            var metadataModel = GenerateMetadataModel(cancellationToken);
            var members = new List<MemberDeclarationSyntax>();

            foreach (var type in metadataModel.InvokableInterfaces)
            {
                foreach (var method in type.Methods)
                {
                    var (invokable, generatedInvokerDescription) = InvokableGenerator.Generate(this, _libraryTypes, type, method);
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
                members.Add(SerializerGenerator.GenerateSerializer(_libraryTypes, type, partialTypeSerializers));

                // Generate a copier for each serializable type.
                members.Add(DeepCopierGenerator.GenerateCopier(_libraryTypes, type));

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
                            .AddArgumentListArguments(AttributeArgument(TypeOfExpression(ParseTypeName($"{GeneratedNamespaceName}.{metadataClass.Identifier.Text}"))))));

            return CompilationUnit()
                .WithAttributeLists(List(new[] { metadataAttribute }))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(ParseName(GeneratedNamespaceName))
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
                        var typeDescription = new SerializableTypeDescription(semanticModel, symbol, GetDataMembers(symbol), _libraryTypes);
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
                            var methods = GetMethods(symbol).ToList();
                            var description = new InvokableInterfaceDescription(
                                _libraryTypes,
                                semanticModel,
                                symbol,
                                GetTypeAlias(symbol) ?? symbol.Name,
                                methods,
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

                        if (symbol.HasAttribute(_libraryTypes.RegisterCopierAttribute))
                        {
                            metadataModel.DetectedCopiers.Add(symbol);
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

        public ushort? GetId(ISymbol memberSymbol)
        {
            var idAttr = memberSymbol.GetAttributes().FirstOrDefault(attr => _libraryTypes.IdAttributeTypes.Any(t => SymbolEqualityComparer.Default.Equals(t, attr.AttributeClass)));
            if (idAttr is null)
            {
                return null;
            }

            var id = (ushort)idAttr.ConstructorArguments.First().Value;
            return id;
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
        private IEnumerable<MethodDescription> GetMethods(INamedTypeSymbol symbol)
        {
            IEnumerable<INamedTypeSymbol> GetAllInterfaces(INamedTypeSymbol s)
            {
                if (s.TypeKind == TypeKind.Interface)
                {
                    yield return s;
                }

                foreach (var i in s.AllInterfaces)
                {
                    yield return i;
                }
            }

#pragma warning disable RS1024 // Compare symbols correctly
            var methods = new Dictionary<IMethodSymbol, bool>(MethodSignatureComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
            foreach (var iface in GetAllInterfaces(symbol))
            {
                foreach (var method in iface.GetDeclaredInstanceMembers<IMethodSymbol>())
                {
                    if (methods.TryGetValue(method, out var description))
                    {
                        methods[method] = true;
                        continue;
                    }

                    methods.Add(method, false);
                }
            }

            var idCounter = 1;
            foreach (var pair in methods.OrderBy(kv => kv.Key, MethodSignatureComparer.Default))
            {
                var method = pair.Key;
                var id = GetId(method) ?? idCounter;
                if (id >= idCounter)
                {
                    idCounter = id + 1;
                }

                yield return new MethodDescription(method, id.ToString(CultureInfo.InvariantCulture), hasCollision: pair.Value);
            }
        }

        private sealed class MethodSignatureComparer : IEqualityComparer<IMethodSymbol>, IComparer<IMethodSymbol>
        {
            public static MethodSignatureComparer Default { get; } = new MethodSignatureComparer();

            private MethodSignatureComparer()
            {
            }

            public bool Equals(IMethodSymbol x, IMethodSymbol y)
            {
                if (!string.Equals(x.Name, y.Name, StringComparison.Ordinal))
                {
                    return false;
                }

                if (x.TypeArguments.Length != y.TypeArguments.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.TypeArguments.Length; i++)
                {
                    if (!SymbolEqualityComparer.Default.Equals(x.TypeArguments[i], y.TypeArguments[i]))
                    {
                        return false;
                    }
                }

                if (x.Parameters.Length != y.Parameters.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.Parameters.Length; i++)
                {
                    if (!SymbolEqualityComparer.Default.Equals(x.Parameters[i].Type, y.Parameters[i].Type))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(IMethodSymbol obj)
            {
                int hashCode = -499943048;
                hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(obj.Name);

                foreach (var arg in obj.TypeArguments)
                {
                    hashCode = hashCode * -1521134295 + SymbolEqualityComparer.Default.GetHashCode(arg);
                }

                foreach (var parameter in obj.Parameters)
                {
                    hashCode = hashCode * -1521134295 + SymbolEqualityComparer.Default.GetHashCode(parameter.Type);
                }

                return hashCode;
            }

            public int Compare(IMethodSymbol x, IMethodSymbol y)
            {
                var result = StringComparer.Ordinal.Compare(x.Name, y.Name);
                if (result != 0)
                {
                    return result;
                }

                result = x.TypeArguments.Length.CompareTo(y.TypeArguments.Length);
                if (result != 0)
                {
                    return result;
                }

                for (var i = 0; i < x.TypeArguments.Length; i++)
                {
                    var xh = SymbolEqualityComparer.Default.GetHashCode(x.TypeArguments[i]);
                    var yh = SymbolEqualityComparer.Default.GetHashCode(y.TypeArguments[i]);
                    result = xh.CompareTo(yh);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                result = x.Parameters.Length.CompareTo(y.Parameters.Length);
                if (result != 0)
                {
                    return result;
                }

                for (var i = 0; i < x.Parameters.Length; i++)
                {
                    var xh = SymbolEqualityComparer.Default.GetHashCode(x.Parameters[i].Type);
                    var yh = SymbolEqualityComparer.Default.GetHashCode(y.Parameters[i].Type);
                    result = xh.CompareTo(yh);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                return 0;
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

        internal static AttributeSyntax GetMethodImplAttributeSyntax()
        {
            return Attribute(ParseName("System.Runtime.CompilerServices.MethodImplAttribute"))
                .AddArgumentListArguments(AttributeArgument(ParseName("System.Runtime.CompilerServices.MethodImplOptions").Member("AggressiveInlining")));
        }
    }
}