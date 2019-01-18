using System.Threading.Tasks;

namespace TestRpc.App
{
    public sealed class PingPongGrain : IPingPongGrain
    {
        public ValueTask Ping() => default;
        public ValueTask<string> Echo(string input) => new ValueTask<string>(input);
    }
}