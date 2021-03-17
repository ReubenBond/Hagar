using Hagar;
using System;

namespace TestRpc.Runtime
{
    [GenerateSerializer]
    public sealed class Message : IDisposable
    {
        [Id(0)]
        public int MessageId { get; set; }
        [Id(1)]
        public GrainId Target { get; set; }
        [Id(2)]
        public GrainId Source { get; set; }
        [Id(3)]
        public int Direction { get; set; }
        [Id(4)]
        public object Body { get; set; }

        public void Dispose()
        {
            if (Body is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Body = default;
            Direction = default;
            MessageId = default;
            Source = default;
            Target = default;
            MessagePool.Return(this);
        }
    }
}