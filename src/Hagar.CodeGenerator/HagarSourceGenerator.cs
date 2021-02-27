using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;

namespace Hagar.CodeGenerator
{
    [Generator]
    public class HagarSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_attachdebugger", out var attachDebuggerOption)
                && string.Equals("true", attachDebuggerOption, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debugger.Launch();
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.hagar_designtimebuild", out var isDesignTimeBuild)
                && string.Equals("true", isDesignTimeBuild, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var codeGenerator = new CodeGenerator(context.Compilation, new CodeGeneratorOptions());
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