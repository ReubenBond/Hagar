using Hagar;

namespace TestRpc.Runtime
{
    [GenerateMethodSerializers(typeof(ProxyBase))]
    public interface IGrain
    {
    }
}