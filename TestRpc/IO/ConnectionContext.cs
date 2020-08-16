using System.IO.Pipelines;

namespace TestRpc.IO
{
    internal sealed class ConnectionContext : IDuplexPipe
    {
        public ConnectionContext(PipeReader input, PipeWriter output)
        {
            Input = input;
            Output = output;
        }

        public PipeReader Input { get; }
        public PipeWriter Output { get; }
    }
}