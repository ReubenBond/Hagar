using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class MethodDescription
    {
        public MethodDescription(IMethodSymbol method)
        {
            this.Method = method;
        }

        public IMethodSymbol Method { get; }

        public override int GetHashCode() => this.Method.GetHashCode();
    }
}