using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator.SyntaxGeneration
{
    internal static class SymbolExtensions
    {
        public static TypeSyntax ToTypeSyntax(this ITypeSymbol typeSymbol) => ParseTypeName(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        public static TypeSyntax ToTypeSyntax(this ITypeSymbol typeSymbol, params TypeSyntax[] genericParameters)
        {
            var displayString = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var nameSyntax = ParseName(displayString);

            switch (nameSyntax)
            {
                case AliasQualifiedNameSyntax aliased:
                    return aliased.WithName(WithGenericParameters(aliased.Name));
                case QualifiedNameSyntax qualified:
                    return qualified.WithRight(WithGenericParameters(qualified.Right));
                case GenericNameSyntax g:
                    return WithGenericParameters(g);
                default:
                    ThrowInvalidOperationException();
                    return default;
            }

            SimpleNameSyntax WithGenericParameters(SimpleNameSyntax simpleNameSyntax)
            {
                if (simpleNameSyntax is GenericNameSyntax generic)
                {
                    return generic.WithTypeArgumentList(TypeArgumentList(SeparatedList(genericParameters)));
                }

                ThrowInvalidOperationException();
                return default;
            }

            void ThrowInvalidOperationException()
            {
                throw new InvalidOperationException(
                    $"Attempted to add generic parameters to non-generic type {displayString} ({nameSyntax.GetType()}, adding parameters {string.Join(", ", genericParameters.Select(n => n.ToFullString()))}");
            }
        }

        public static TypeSyntax ToOpenTypeSyntax(this INamedTypeSymbol typeSymbol)
        {
            var displayString = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var nameSyntax = ParseName(displayString);

            switch (nameSyntax)
            {
                case AliasQualifiedNameSyntax aliased:
                    return aliased.WithName(WithGenericParameters(aliased.Name, typeSymbol.Arity));
                case QualifiedNameSyntax qualified:
                    return qualified.WithRight(WithGenericParameters(qualified.Right, typeSymbol.Arity));
                case GenericNameSyntax g:
                    return WithGenericParameters(g, typeSymbol.Arity);
                default:
                    return nameSyntax;
            }

            static SimpleNameSyntax WithGenericParameters(SimpleNameSyntax simpleNameSyntax, int arity)
            {
                if (simpleNameSyntax is GenericNameSyntax generic)
                {
                    return generic.WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>(Enumerable.Range(0, arity).Select(_ => OmittedTypeArgument()))));
                }

                return simpleNameSyntax;
            }
        }

        public static NameSyntax ToNameSyntax(this ITypeSymbol typeSymbol) => ParseName(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        public static string GetValidIdentifier(this ITypeSymbol type) => type switch
        {
            INamedTypeSymbol named when !named.IsGenericType => $"{named.Name}",
            INamedTypeSymbol named => $"{named.Name}_{string.Join("_", named.TypeArguments.Select(GetValidIdentifier))}",
            IArrayTypeSymbol array => $"{GetValidIdentifier(array.ElementType)}_{array.Rank}",
            IPointerTypeSymbol pointer => $"{GetValidIdentifier(pointer.PointedAtType)}_ptr",
            ITypeParameterSymbol parameter => $"{parameter.Name}",
            _ => throw new NotSupportedException($"Unable to format type of kind {type.GetType()} with name \"{type.Name}\""),
        };

        public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
        {
            var attributes = symbol.GetAttributes();
            foreach (var attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}