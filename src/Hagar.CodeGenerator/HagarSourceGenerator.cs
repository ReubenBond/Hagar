using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace Hagar.CodeGenerator
{
    [Generator]
    public class HagarSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_designtimebuild", out var isDesignTimeBuild)
                && string.Equals("true", isDesignTimeBuild, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_attachdebugger", out var attachDebuggerOption)
                && string.Equals("true", attachDebuggerOption, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debugger.Launch();
            }

            var options = new CodeGeneratorOptions();
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_immutableattributes", out var immutableAttributes) && immutableAttributes is {Length: > 0 })
            {
                options.ImmutableAttributes.AddRange(immutableAttributes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList());
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_aliasattributes", out var aliasAttributes) && aliasAttributes is {Length: > 0 })
            {
                options.AliasAttributes.AddRange(aliasAttributes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList());
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_idattributes", out var idAttributes) && idAttributes is {Length: > 0 })
            {
                options.IdAttributes.AddRange(idAttributes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList());
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_generateserializerattributes", out var generateSerializerAttributes) && generateSerializerAttributes is {Length: > 0 })
            {
                options.GenerateSerializerAttributes.AddRange(generateSerializerAttributes.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList());
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_generatefieldids", out var generateFieldIds) && generateFieldIds is {Length: > 0 })
            {
                options.GenerateFieldIds = bool.Parse(generateFieldIds);
            }

            var codeGenerator = new CodeGenerator(context.Compilation, options);
            var syntax = codeGenerator.GenerateCode(context.CancellationToken);
            var sourceString = syntax.NormalizeWhitespace().ToFullString();
            var sourceText = SourceText.From(sourceString, Encoding.UTF8);
            context.AddSource("Hagar.g.cs", sourceText);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}