using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Hagar.CodeGenerator
{
    [Generator]
    public class HagarSourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            var codeGenerator = new CodeGenerator(context.Compilation, new CodeGeneratorOptions());
            var syntax = codeGenerator.GenerateCode(context.CancellationToken);
            var sourceString = syntax.NormalizeWhitespace().ToFullString();
            var sourceText = SourceText.From(sourceString, Encoding.UTF8);
            context.AddSource("Hagar.g.cs", sourceText);
        }

        public void Initialize(InitializationContext context)
        {
        }
    }
}