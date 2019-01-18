using System.IO.Pipelines;

namespace TestRpc.IO
{
    internal sealed class ConnectionContext : IDuplexPipe
    {
        public ConnectionContext(PipeReader input, PipeWriter output)
        {
            this.Input = input;
            this.Output = output;
        }

        public PipeReader Input { get; }
        public PipeWriter Output { get; }
    }
}
