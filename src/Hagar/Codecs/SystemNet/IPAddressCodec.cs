using Hagar.Serializers;
using System.Net;

namespace Hagar.Codecs.SystemNet
{
    [RegisterSerializer]
    public sealed class IPAddressCodec : GeneralizedReferenceTypeSurrogateCodec<IPAddress, IPAddressSurrogate>
    {
        public IPAddressCodec(IValueSerializer<IPAddressSurrogate> surrogateSerializer) : base(surrogateSerializer) { }

        public override IPAddress ConvertFromSurrogate(ref IPAddressSurrogate surrogate) => new IPAddress(surrogate.AddressBytes);

        public override void ConvertToSurrogate(IPAddress value, ref IPAddressSurrogate surrogate) => surrogate = new IPAddressSurrogate
        {
            AddressBytes = value.GetAddressBytes()
        };
    }

    [RegisterSerializer]
    public sealed class IPEndPointCodec : GeneralizedReferenceTypeSurrogateCodec<IPEndPoint, IPEndPointSurrogate>
    {
        public IPEndPointCodec(IValueSerializer<IPEndPointSurrogate> surrogateSerializer) : base(surrogateSerializer) { }

        public override IPEndPoint ConvertFromSurrogate(ref IPEndPointSurrogate surrogate) => new IPEndPoint(new IPAddress(surrogate.AddressBytes), surrogate.Port);

        public override void ConvertToSurrogate(IPEndPoint value, ref IPEndPointSurrogate surrogate) => surrogate = new IPEndPointSurrogate
        {
            AddressBytes = value.Address.GetAddressBytes(),
            Port = (ushort)value.Port
        };
    }

    [GenerateSerializer]
    public struct IPAddressSurrogate
    {
        [Id(1)]
        public byte[] AddressBytes;
    }

    [GenerateSerializer]
    public struct IPEndPointSurrogate
    {
        [Id(1)]
        public byte[] AddressBytes;

        [Id(2)]
        public ushort Port;
    }
}
