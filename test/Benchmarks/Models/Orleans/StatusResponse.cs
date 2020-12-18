using System;
using System.Collections.Generic;

namespace FakeFx.Runtime
{
    [Serializable]
    [Hagar.GenerateSerializer]
    internal class StatusResponse
    {
        [Hagar.Id(1)]
        private readonly uint _statusFlags;

        public StatusResponse(bool isExecuting, bool isWaiting, List<string> diagnostics)
        {
            if (isExecuting)
            {
                _statusFlags |= 0x1;
            }

            if (isWaiting)
            {
                _statusFlags |= 0x2;
            }

            Diagnostics = diagnostics;
        }

        [Hagar.Id(2)]
        public List<string> Diagnostics { get; }

        public bool IsExecuting => (_statusFlags & 0x1) != 0;

        public bool IsWaiting => (_statusFlags & 0x2) != 0;

        public override string ToString() => $"IsExecuting: {IsExecuting}, IsWaiting: {IsWaiting}, Diagnostics: [{string.Join(", ", this.Diagnostics)}]";
    }
}
