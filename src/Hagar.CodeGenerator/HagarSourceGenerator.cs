using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Hagar.CodeGenerator
{
    [Generator]
    public class HagarSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var codeGenerator = new CodeGenerator(context.Compilation, new CodeGeneratorOptions());
            var syntax = codeGenerator.GenerateCode(context.CancellationToken);
            var sourceString = syntax.NormalizeWhitespace().ToFullString();
            var sourceText = SourceText.From(sourceString, Encoding.UTF8);
            context.AddSource("Hagar.g.cs", sourceText);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if false 
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif 
        }
    }
}