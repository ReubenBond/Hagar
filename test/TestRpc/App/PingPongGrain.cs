using System;
using System.Threading.Tasks;

namespace TestRpc.App
{
    public sealed class PingPongGrain : IPingPongGrain
    {
        public ValueTask Ping() => default;
        public ValueTask<string> Echo(string input)
        {
            Console.WriteLine($"Received call to PingPongGrain.Echo(\"{input}\") -> sending response");
            return new ValueTask<string>(input);
        }
    }
}