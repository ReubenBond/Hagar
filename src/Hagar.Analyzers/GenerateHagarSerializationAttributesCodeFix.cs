using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.Analyzers
{
    internal static class Constants
    {
        public const string IdAttributeName = "Id";
        public const string IdAttributeFullyQualifiedName = "Hagar.IdAttribute";
        public const string GenerateSerializerAttributeName = "GenerateSerializer";
        public const string NonSerializedAttribute = "NonSerialized";
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GenerateHagarSerializationAttributesAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "HAGAR0001";
        private const string Category = "Usage";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AddSerializationAttributesTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AddSerializationAttributesMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AddSerializationAttributesDescription), Resources.ResourceManager, typeof(Resources));

        internal static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(RuleId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(CheckSyntaxNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private void CheckSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is TypeDeclarationSyntax typeDeclaration && !typeDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.StaticKeyword))
            {
                if (typeDeclaration.HasAttribute(Constants.GenerateSerializerAttributeName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
                }
            }
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class GenerateHagarSerializationAttributesCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(GenerateHagarSerializationAttributesAnalyzer.RuleId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer; 

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var declaration = root.FindNode(context.Span).FirstAncestorOrSelf<TypeDeclarationSyntax>();
            context.RegisterCodeFix(
                CodeAction.Create("Generate serialization attributes", cancellationToken => AddSerializationAttributes(declaration, context, cancellationToken), equivalenceKey: GenerateHagarSerializationAttributesAnalyzer.RuleId),
                context.Diagnostics[0]);
        }

        private async Task<Document> AddSerializationAttributes(TypeDeclarationSyntax declaration, CodeFixContext context, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
            uint nextId = 1;
            var serializableMembers = new List<MemberDeclarationSyntax>();
            foreach (var member in declaration.Members)
            {
                if (!member.IsInstanceMember())
                {
                    continue;
                }

                if (!member.IsFieldOrAutoProperty())
                {
                    continue;
                }

                // Skip members with existing [Id(x)] atttributes, but record the highest value of x so that newly added attributes can begin from that value.
                if (member.TryGetAttribute(Constants.IdAttributeName, out var attribute))
                {
                    var args = attribute.ArgumentList?.Arguments;
                    if (args.HasValue)
                    {
                        if (args.Value.Count > 0)
                        {
                            var idArg = args.Value[0];
                            if (idArg.Expression is LiteralExpressionSyntax literalExpression
                                && uint.TryParse(literalExpression.Token.ValueText, out var value)
                                && value >= nextId)
                            {
                                nextId = value + 1;
                            }
                        }
                    }

                    continue;
                }

                if (member.HasAttribute(Constants.NonSerializedAttribute))
                {
                    // No need to add any attribute.
                    continue;
                }

                serializableMembers.Add(member);
            }

            foreach (var member in serializableMembers)
            {
                // Add the [Id(x)] attribute
                var attribute = Attribute(ParseName(Constants.IdAttributeFullyQualifiedName))
                    .AddArgumentListArguments(AttributeArgument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal((int)nextId++))))
                    .WithAdditionalAnnotations(Simplifier.Annotation);
                editor.AddAttribute(member, attribute);
            }

            return editor.GetChangedDocument();
        }
    }

    internal static class SyntaxHelpers
    {
        public static string GetTypeName(this AttributeSyntax attributeSyntax) => attributeSyntax.Name switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            _ => throw new NotSupportedException()
        };

        public static bool IsAttribute(this AttributeSyntax attributeSyntax, string attributeName)
        {
            var name = attributeSyntax.GetTypeName();
            return string.Equals(name, attributeName, StringComparison.Ordinal)
                || (name.StartsWith(attributeName) && name.EndsWith(nameof(Attribute)) && name.Length == attributeName.Length + nameof(Attribute).Length);
        }

        public static bool HasAttribute(this MemberDeclarationSyntax member, string attributeName)
        {
            foreach (var list in member.AttributeLists)
            {
                foreach (var attr in list.Attributes)
                {
                    if (attr.IsAttribute(attributeName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetAttribute(this MemberDeclarationSyntax member, string attributeName, out AttributeSyntax attribute)
        {
            foreach (var list in member.AttributeLists)
            {
                foreach (var attr in list.Attributes)
                {
                    if (attr.IsAttribute(attributeName))
                    {
                        attribute = attr;
                        return true;
                    }
                }
            }

            attribute = default;
            return false;
        }

        public static bool IsInstanceMember(this MemberDeclarationSyntax member)
        {
            foreach (var modifier in member.Modifiers)
            {
                if (modifier.Kind() == SyntaxKind.StaticKeyword)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsFieldOrAutoProperty(this MemberDeclarationSyntax member)
        {
            bool isFieldOrAutoProperty = false;
            switch (member)
            {
                case FieldDeclarationSyntax:
                    isFieldOrAutoProperty = true;
                    break;
                case PropertyDeclarationSyntax property:
                    {
                        bool hasExpressionBody = property.ExpressionBody is object;
                        var accessors = property.AccessorList?.Accessors;
                        if (!hasExpressionBody && accessors.HasValue)
                        {
                            foreach (var accessor in accessors)
                            {
                                if (accessor.ExpressionBody is object)
                                {
                                    hasExpressionBody = true;
                                    break;
                                }
                            }
                        }

                        if (!hasExpressionBody)
                        {
                            isFieldOrAutoProperty = true;
                        }

                        break;
                    }
            }

            return isFieldOrAutoProperty;
        }

    }
}
