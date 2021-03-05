using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class MethodDescription
    {
        public MethodDescription(IMethodSymbol method, string name, bool hasCollision)
        {
            Method = method;
            Name = name;
            HasCollision = hasCollision;
        }

        public string Name { get; }

        public IMethodSymbol Method { get; }
        public bool HasCollision { get; }

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Method);
    }
}