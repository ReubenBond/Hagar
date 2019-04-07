namespace Hagar.CodeGenerator
{
    internal interface IGeneratedInvokerDescription : ISerializableTypeDescription
    {
        IInvokableInterfaceDescription InterfaceDescription { get; }
    }
}