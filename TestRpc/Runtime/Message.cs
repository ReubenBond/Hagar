using System;
using Hagar;

namespace TestRpc.Runtime
{
    [GenerateSerializer]
    public sealed class Message : IDisposable
    {
        [Id(0)]
        public int MessageId { get; set; }
        [Id(1)]
        public ActivationId Target { get; set; }
        [Id(2)]
        public ActivationId Source { get; set; }
        [Id(3)]
        public int Direction { get; set; }
        [Id(4)]
        public object Body { get; set; }

        public void Dispose()
        {
            if (Body is IDisposable disposable) disposable.Dispose();
            this.Body = default;
            this.Direction = default;
            this.MessageId = default;
            this.Source = default;
            this.Target = default;
            MessagePool.Return(this);
        }
    }
}