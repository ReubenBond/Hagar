using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Hagar.CodeGenerator
{
    internal class MetadataModel
    {
        public List<ISerializableTypeDescription> SerializableTypes { get; } =
            new List<ISerializableTypeDescription>(1024);

        public List<IInvokableInterfaceDescription> InvokableInterfaces { get; } =
            new List<IInvokableInterfaceDescription>(1024);
        public Dictionary<MethodDescription, IGeneratedInvokerDescription> GeneratedInvokables { get; } = new Dictionary<MethodDescription, IGeneratedInvokerDescription>();
        public List<IGeneratedProxyDescription> GeneratedProxies { get; } = new List<IGeneratedProxyDescription>(1024);
        public List<ISerializableTypeDescription> ActivatableTypes { get; } =
            new List<ISerializableTypeDescription>(1024);
        public List<INamedTypeSymbol> DetectedSerializers { get; } = new List<INamedTypeSymbol>();
        public List<INamedTypeSymbol> DetectedActivators { get; } = new List<INamedTypeSymbol>();
    }
}