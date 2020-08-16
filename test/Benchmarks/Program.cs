using BenchmarkDotNet.Running;
using Benchmarks.Comparison;

namespace Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "loop")
            {
                var benchmarks = new ClassSerializeBenchmark();
                while (true)
                {
                    _ = benchmarks.Hagar();
                }
            }

            if (args.Length > 0 && args[0] == "structloop")
            {
                var benchmarks = new StructSerializeBenchmark();
                while (true)
                {
                    _ = benchmarks.Hagar();
                }
            }

            if (args.Length > 0 && args[0] == "dstructloop")
            {
                var benchmarks = new StructDeserializeBenchmark();
                while (true)
                {
                    _ = benchmarks.Hagar();
                }
            }

            if (args.Length > 0 && args[0] == "dloop")
            {
                var benchmarks = new ClassDeserializeBenchmark();
                while (true)
                {
                    _ = benchmarks.Hagar();
                }
            }

            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(ClassDeserializeBenchmark),
                typeof(ClassSerializeBenchmark),
                typeof(StructSerializeBenchmark),
                typeof(StructDeserializeBenchmark),
                typeof(ComplexTypeBenchmarks),
                typeof(FieldHeaderBenchmarks)
            });

            _ = switcher.Run(args);
        }
    }
}