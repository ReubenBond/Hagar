using System;
using Hagar;

namespace Benchmarks
{
    [Serializable]
    [GenerateSerializer]
    public class SimpleClass
    {
        [Id(0)]
        public int BaseInt { get; set; }
    }
}