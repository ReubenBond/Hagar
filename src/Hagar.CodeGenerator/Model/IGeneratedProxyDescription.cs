using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hagar.CodeGenerator
{
    internal interface IGeneratedProxyDescription
    {
        TypeSyntax TypeSyntax { get; }
        IInvokableInterfaceDescription InterfaceDescription { get; }
    }
}