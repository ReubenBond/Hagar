﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.Analyzers
{
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
            context.RegisterCodeFix(
                CodeAction.Create("Mark properties and fields [NonSerialized]", cancellationToken => AddNonSerializedAttributes(root, declaration, context, cancellationToken), equivalenceKey: GenerateHagarSerializationAttributesAnalyzer.RuleId + "NonSerialized"),
                context.Diagnostics[0]);
        }

        private static async Task<Document> AddSerializationAttributes(TypeDeclarationSyntax declaration, CodeFixContext context, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
            var (serializableMembers, nextId) = SerializationAttributesHelper.AnalyzeTypeDeclaration(declaration);

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

        private static async Task<Document> AddNonSerializedAttributes(SyntaxNode root, TypeDeclarationSyntax declaration, CodeFixContext context, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
            var (serializableMembers, _) = SerializationAttributesHelper.AnalyzeTypeDeclaration(declaration);

            var insertUsingDirective = true;
            var ns = root.DescendantNodesAndSelf()
                .OfType<UsingDirectiveSyntax>()
                .FirstOrDefault(directive => string.Equals(directive.Name.ToString(), Constants.SystemNamespace));
            if (ns is object)
            {
                insertUsingDirective = false;
            }

            if (insertUsingDirective)
            {
                var usingDirective = UsingDirective(IdentifierName(Constants.SystemNamespace)).WithTrailingTrivia(EndOfLine("\r\n"));
                var lastUsing = root.DescendantNodesAndSelf().OfType<UsingDirectiveSyntax>().LastOrDefault();
                if (lastUsing is object)
                {
                    editor.InsertAfter(lastUsing, usingDirective);
                }
                else if (root.DescendantNodesAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault() is NamespaceDeclarationSyntax firstNamespace)
                {
                    editor.InsertBefore(lastUsing, usingDirective);
                }
                else if (root.DescendantNodesAndSelf().FirstOrDefault() is SyntaxNode firstNode)
                {
                    editor.InsertBefore(firstNode, usingDirective);
                }
            }
            
            foreach (var member in serializableMembers)
            {
                // Add the [NonSerialized] attribute 
                var attribute = AttributeList().AddAttributes(Attribute(ParseName(Constants.NonSerializedAttributeFullyQualifiedName)).WithAdditionalAnnotations(Simplifier.Annotation));

                // Since [NonSerialized] is a field-only attribute, add the field target specifier.
                if (member is PropertyDeclarationSyntax)
                {
                    attribute = attribute.WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.FieldKeyword)));
                }

                editor.AddAttribute(member, attribute);
            }

            return editor.GetChangedDocument();
        }
    }
}
