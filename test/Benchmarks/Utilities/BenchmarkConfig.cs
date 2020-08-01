using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;

namespace Benchmarks.Utilities
{
    internal class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            this.ArtifactsPath = ".\\BenchmarkDotNet.Aritfacts." + DateTime.Now.ToString("u").Replace(' ', '_').Replace(':', '-');
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}