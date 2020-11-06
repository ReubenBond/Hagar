﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Hagar.Analyzers
{
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
            if (context.Node is TypeDeclarationSyntax declaration && !declaration.Modifiers.Any(m => m.Kind() == SyntaxKind.StaticKeyword))
            {
                if (declaration.HasAttribute(Constants.GenerateSerializerAttributeName) || declaration.HasAttribute(Constants.SerializableAttributeName))
                {
                    var (serializableMembers, nextId) = SerializationAttributesHelper.AnalyzeTypeDeclaration(declaration);
                    if (serializableMembers.Count > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
                    }
                }
            }
        }
    }
}
