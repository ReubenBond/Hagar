using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class MetadataModel
    {
        public List<ISerializableTypeDescription> SerializableTypes { get; } =
            new List<ISerializableTypeDescription>(1024);

        public List<IInvokableInterfaceDescription> InvokableInterfaces { get; } =
            new List<IInvokableInterfaceDescription>(1024);
        public Dictionary<MethodDescription, IGeneratedInvokerDescription> GeneratedInvokables { get; } = new Dictionary<MethodDescription, IGeneratedInvokerDescription>();
        public List<IGeneratedProxyDescription> GeneratedProxies { get; } = new List<IGeneratedProxyDescription>(1024);
    }

    internal interface IMemberDescription
    {
        uint FieldId { get; }
        ISymbol Member { get; }
        ITypeSymbol Type { get; }
        string Name { get; }
    }

    internal class FieldDescription : IMemberDescription
    {
        public FieldDescription(uint fieldId, IFieldSymbol field)
        {
            this.FieldId = fieldId;
            this.Field = field;
        }

        public IFieldSymbol Field { get; }
        public uint FieldId { get; }
        public ISymbol Member => this.Field;
        public ITypeSymbol Type => this.Field.Type;
        public string Name => this.Field.Name;
    }

    internal class PropertyDescription : IMemberDescription
    {
        public PropertyDescription(uint fieldId, IPropertySymbol property)
        {
            this.FieldId = fieldId;
            this.Property = property;
        }

        public uint FieldId { get; }
        public ISymbol Member => this.Property;
        public ITypeSymbol Type => this.Property.Type;
        public IPropertySymbol Property { get; }
        public string Name => this.Property.Name;
    }

    internal interface ISerializableTypeDescription
    {
        TypeSyntax TypeSyntax { get; }
        TypeSyntax UnboundTypeSyntax { get; }
        bool HasComplexBaseType { get; }
        INamedTypeSymbol BaseType { get; }
        string Name { get; }
        bool IsValueType { get; }
        bool IsGenericType { get; }
        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }
        List<IMemberDescription> Members { get; }
    }

    internal interface IGeneratedInvokerDescription : ISerializableTypeDescription
    {
        IInvokableInterfaceDescription InterfaceDescription { get; }
    }

    internal interface IInvokableInterfaceDescription
    {
        INamedTypeSymbol InterfaceType { get; }
        List<MethodDescription> Methods { get; }
        INamedTypeSymbol ProxyBaseType { get; }
        bool IsExtension { get; }
    }


    internal interface IGeneratedProxyDescription
    {
        TypeSyntax TypeSyntax { get; }
        IInvokableInterfaceDescription InterfaceDescription { get; }
    }

    internal class InvokableInterfaceDescription : IInvokableInterfaceDescription
    {
        public InvokableInterfaceDescription(
            LibraryTypes libraryTypes,
            INamedTypeSymbol interfaceType,
            IEnumerable<MethodDescription> methods,
            INamedTypeSymbol proxyBaseType,
            bool isExtension)
        {
            this.ValidateBaseClass(libraryTypes, proxyBaseType);
            this.InterfaceType = interfaceType;
            this.ProxyBaseType = proxyBaseType;
            this.IsExtension = isExtension;
            this.Methods = methods.ToList();
        }

        void ValidateBaseClass(LibraryTypes l, INamedTypeSymbol baseClass)
        {
            var found = false;
            foreach (var member in baseClass.GetMembers("Invoke"))
            {
                if (!(member is IMethodSymbol method)) continue;
                if (method.TypeParameters.Length != 1) continue;
                if (method.Parameters.Length != 1) continue;
                if (!method.Parameters[0].Type.Equals(method.TypeParameters[0])) continue;
                if (!method.TypeParameters[0].ConstraintTypes.Contains(l.IInvokable)) continue;
                if (!method.ReturnType.Equals(l.ValueTask)) continue;
                found = true;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"Proxy base class {baseClass} does not contain a definition for ValueTask Invoke<T>(T) where T : IInvokable");
            }
        }

        public INamedTypeSymbol InterfaceType { get; }
        public List<MethodDescription> Methods { get; }
        public INamedTypeSymbol ProxyBaseType { get; }
        public bool IsExtension { get; }
    }

    internal class SerializableTypeDescription : ISerializableTypeDescription
    {
        public SerializableTypeDescription(INamedTypeSymbol type, IEnumerable<IMemberDescription> members)
        {
            this.Type = type;
            this.Members = members.ToList();
        }

        private INamedTypeSymbol Type { get; }

        public TypeSyntax TypeSyntax => this.Type.ToTypeSyntax();
        public TypeSyntax UnboundTypeSyntax => this.Type.ToTypeSyntax();

        public bool HasComplexBaseType => !this.IsValueType &&
                                          this.Type.BaseType != null &&
                                          this.Type.BaseType.SpecialType != SpecialType.System_Object;

        public INamedTypeSymbol BaseType => this.Type.BaseType;

        public string Name => this.Type.Name;

        public bool IsValueType => this.Type.IsValueType;

        public bool IsGenericType => this.Type.IsGenericType;

        public ImmutableArray<ITypeParameterSymbol> TypeParameters => this.Type.TypeParameters;

        public List<IMemberDescription> Members { get; }
    }

    internal class MethodDescription
    {
        public MethodDescription(IMethodSymbol method)
        {
            this.Method = method;
        }

        public IMethodSymbol Method { get; }

        public override int GetHashCode() => this.Method.GetHashCode();
    }

    public class CodeGenerator
    {
        internal const string CodeGeneratorName = "HagarGen";
        private readonly Compilation compilation;
        private readonly LibraryTypes libraryTypes;

        public CodeGenerator(Compilation compilation)
        {
            this.compilation = compilation;
            this.libraryTypes = LibraryTypes.FromCompilation(compilation);
        }

        public async Task<CompilationUnitSyntax> GenerateCode(CancellationToken cancellationToken)
        {
            var namespaceName = "HagarGeneratedCode." + this.compilation.AssemblyName;

            // Collect metadata from the compilation.
            var metadataModel = await this.GenerateMetadataModel(cancellationToken);
            var members = new List<MemberDeclarationSyntax>();

            foreach (var type in metadataModel.InvokableInterfaces)
            {
                foreach (var method in type.Methods)
                {
                    var (invokable, generatedInvokerDescription) = InvokableGenerator.Generate(this.compilation, this.libraryTypes, type, method);
                    metadataModel.SerializableTypes.Add(generatedInvokerDescription);
                    metadataModel.GeneratedInvokables[method] = generatedInvokerDescription;
                    members.Add(invokable);
                }

                var (proxy, generatedProxyDescription) = ProxyGenerator.Generate(this.compilation, this.libraryTypes, type, metadataModel);
                metadataModel.GeneratedProxies.Add(generatedProxyDescription);
                members.Add(proxy);
            }

            // Generate code.
            foreach (var type in metadataModel.SerializableTypes)
            {
                // Generate a partial serializer class for each serializable type.
                members.Add(SerializerGenerator.GenerateSerializer(this.compilation, this.libraryTypes, type));
            }
            
            // Generate metadata.
            var metadataClass = MetadataGenerator.GenerateMetadata(this.compilation, metadataModel);
            members.Add(metadataClass);

            var metadataAttribute = AttributeList()
                .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword)))
                .WithAttributes(
                    SingletonSeparatedList(
                        Attribute(this.libraryTypes.MetadataProviderAttribute.ToNameSyntax())
                            .AddArgumentListArguments(AttributeArgument(TypeOfExpression(ParseTypeName($"{namespaceName}.{metadataClass.Identifier.Text}"))))));

            return CompilationUnit()
                .WithAttributeLists(List(new []{metadataAttribute}))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(ParseName(namespaceName))
                        .WithMembers(List(members))
                        .WithUsings(List(new[] {UsingDirective(ParseName("global::Hagar.Codecs")), UsingDirective(ParseName("global::Hagar.GeneratedCodeHelpers")) }))));
        }

        private async Task<MetadataModel> GenerateMetadataModel(CancellationToken cancellationToken)
        {
            var metadataModel = new MetadataModel();

            foreach (var syntaxTree in this.compilation.SyntaxTrees)
            {
                var semanticModel = this.compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: false);
                var rootNode = await syntaxTree.GetRootAsync(cancellationToken);
                foreach (var node in GetTypeDeclarations(rootNode))
                {
                    var symbol = semanticModel.GetDeclaredSymbol(node);

                    if (this.HasAttribute(symbol, this.libraryTypes.GenerateSerializerAttribute, inherited: true) != null)
                    {
                        var typeDescription = new SerializableTypeDescription(symbol, this.GetDataMembers(symbol));
                        metadataModel.SerializableTypes.Add(typeDescription);
                    }

                    if (symbol.TypeKind == TypeKind.Interface)
                    {
                        var attribute = this.HasAttribute(
                            symbol,
                            this.libraryTypes.GenerateMethodSerializersAttribute,
                            inherited: true);
                        if (attribute != null)
                        {
                            var baseClass = (INamedTypeSymbol)attribute.ConstructorArguments[0].Value;
                            var isExtension = (bool)attribute.ConstructorArguments[1].Value;
                            var description = new InvokableInterfaceDescription(
                                this.libraryTypes,
                                symbol,
                                this.GetMethods(symbol),
                                baseClass,
                                isExtension);
                            metadataModel.InvokableInterfaces.Add(description);
                        }
                    }
                }
            }

            return metadataModel;
        }

        private static IEnumerable<TypeDeclarationSyntax> GetTypeDeclarations(SyntaxNode node)
        {
            SyntaxList<MemberDeclarationSyntax> members;
            switch (node)
            {
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
            foreach (var member in symbol.GetMembers())
            {
                // Only consider fields and properties.
                if (!(member is IFieldSymbol || member is IPropertySymbol)) continue;

                var idAttr = member.GetAttributes().SingleOrDefault(attr => attr.AttributeClass.Equals(this.libraryTypes.IdAttribute));
                if (idAttr == null) continue;
                var id = (uint)idAttr.ConstructorArguments.First().Value;

                if (member is IPropertySymbol prop)
                {
                    if (prop.IsReadOnly || prop.IsWriteOnly)
                    {
                        // TODO: add diagnostic: read-only property
                        continue;
                    }

                    yield return new PropertyDescription(id, prop);
                }

                if (member is IFieldSymbol field)
                {
                    if (field.IsConst || field.IsReadOnly)
                    {
                        // TODO: add diagnostic: read-only field 
                        continue;
                    }

                    yield return new FieldDescription(id, field);
                }
            }
        }

        // Returns descriptions of all methods 
        private IEnumerable<MethodDescription> GetMethods(INamedTypeSymbol symbol)
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
        private AttributeData HasAttribute(INamedTypeSymbol symbol, ISymbol attributeType, bool inherited = false)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass.Equals(attributeType)) return attribute;
            }

            if (inherited)
            {
                foreach (var iface in symbol.AllInterfaces)
                {
                    foreach (var attribute in iface.GetAttributes())
                    {
                        if (attribute.AttributeClass.Equals(attributeType)) return attribute;
                    }
                }

                while ((symbol = symbol.BaseType) != null)
                {
                    foreach (var attribute in symbol.GetAttributes())
                    {
                        if (attribute.AttributeClass.Equals(attributeType)) return attribute;
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