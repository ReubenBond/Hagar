using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator.SyntaxGeneration
{
    internal static class SyntaxFactoryUtility
    {
        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, string member)
        {
            return instance.Member(member.ToIdentifierName());
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, IdentifierNameSyntax member)
        {
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, member);
        }

        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, GenericNameSyntax member)
        {
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, member);
        }

        public static MemberAccessExpressionSyntax Member(
            this ExpressionSyntax instance,
            string member,
            params TypeSyntax[] genericTypes)
        {
            return
                instance.Member(
                    member.ToGenericName()
                        .AddTypeArgumentListArguments(genericTypes));
        }

        public static GenericNameSyntax ToGenericName(this string identifier)
        {
            return GenericName(identifier.ToIdentifier());
        }
    }
}