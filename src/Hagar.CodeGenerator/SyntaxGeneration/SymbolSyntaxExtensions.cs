using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator.SyntaxGeneration
{
    internal static class SymbolSyntaxExtensions
    {
        public static ParenthesizedExpressionSyntax GetBindingFlagsParenthesizedExpressionSyntax(
            SyntaxKind operationKind,
            params BindingFlags[] bindingFlags)
        {
            if (bindingFlags.Length < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bindingFlags),
                    $"Can't create parenthesized binary expression with {bindingFlags.Length} arguments");
            }

            var flags = AliasQualifiedName("global", IdentifierName("System")).Member("Reflection").Member("BindingFlags");
            var bindingFlagsBinaryExpression = BinaryExpression(
                operationKind,
                flags.Member(bindingFlags[0].ToString()),
                flags.Member(bindingFlags[1].ToString()));
            for (var i = 2; i < bindingFlags.Length; i++)
            {
                bindingFlagsBinaryExpression = BinaryExpression(
                    operationKind,
                    bindingFlagsBinaryExpression,
                    flags.Member(bindingFlags[i].ToString()));
            }

            return ParenthesizedExpression(bindingFlagsBinaryExpression);
        }
    }
}
