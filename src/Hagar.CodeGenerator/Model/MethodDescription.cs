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

#pragma warning disable RS1024 // Compare symbols correctly
        public override int GetHashCode() => Method.GetHashCode();
#pragma warning restore RS1024 // Compare symbols correctly
    }
}