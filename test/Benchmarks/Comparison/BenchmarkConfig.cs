using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace Benchmarks.Comparison
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
        }
    }
}