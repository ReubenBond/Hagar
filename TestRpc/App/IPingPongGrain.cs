using System.Threading.Tasks;
using TestRpc.Runtime;

namespace TestRpc.App
{
    public interface IPingPongGrain : IGrain
    {
        ValueTask Ping();
        ValueTask<string> Echo(string input);
    }
}