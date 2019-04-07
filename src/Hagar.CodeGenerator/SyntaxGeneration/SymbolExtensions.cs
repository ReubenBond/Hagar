using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator.SyntaxGeneration
{
    internal static class SymbolExtensions
    {
        public static TypeSyntax ToTypeSyntax(this ITypeSymbol typeSymbol)
        {
            return ParseTypeName(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

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

        public static NameSyntax ToNameSyntax(this ITypeSymbol typeSymbol)
        {
            return ParseName(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        public static string GetValidIdentifier(this ITypeSymbol type)
        {
            switch (type)
            {
                case INamedTypeSymbol named when !named.IsGenericType: return $"{named.Name}";
                case INamedTypeSymbol named:
                    return $"{named.Name}_{string.Join("_", named.TypeArguments.Select(GetValidIdentifier))}";
                case IArrayTypeSymbol array:
                    return $"{GetValidIdentifier(array.ElementType)}_{array.Rank}";
                case IPointerTypeSymbol pointer:
                    return $"{GetValidIdentifier(pointer.PointedAtType)}_ptr";
                case ITypeParameterSymbol parameter:
                    return $"{parameter.Name}";
                default:
                    throw new NotSupportedException($"Unable to format type of kind {type.GetType()} with name \"{type.Name}\"");
            }
        }

        public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
        {
            var attributes = symbol.GetAttributes();
            foreach (var attr in attributes)
            {
                if (attr.AttributeClass.Equals(attributeType)) return true;
            }

            return false;
        }
    }
}
