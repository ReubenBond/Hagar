using Hagar;

namespace TestRpc.Runtime
{
    [GenerateMethodSerializers(typeof(ProxyBase), isExtension: true)]
    public interface IGrainExtension
    {
    }
}