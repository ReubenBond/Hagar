using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class MethodDescription
    {
        public MethodDescription(IMethodSymbol method)
        {
            Method = method;
        }

        public IMethodSymbol Method { get; }

        public override int GetHashCode() => Method.GetHashCode();
    }
}