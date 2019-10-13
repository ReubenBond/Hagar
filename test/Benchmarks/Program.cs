using BenchmarkDotNet.Running;
using Benchmarks.Comparison;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "loop")
            {
                var benchmarks = new SerializeBenchmark();
                while (true)
                {
                    benchmarks.Hagar();
                }
            }

            if (args.Length > 0 && args[0] == "structloop")
            {
                var benchmarks = new StructSerializeBenchmark();
                while (true)
                {
                    benchmarks.Hagar();
                }
            }

            if (args.Length > 0 && args[0] == "dstructloop")
            {
                var benchmarks = new StructDeserializeBenchmark();
                while (true)
                {
                    benchmarks.Hagar();
                }
            }

            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(DeserializeBenchmark),
                typeof(SerializeBenchmark),
                typeof(StructSerializeBenchmark),
                typeof(StructDeserializeBenchmark),
                typeof(ComplexTypeBenchmarks),
                typeof(FieldHeaderBenchmarks)
            });

            switcher.Run(args);
        }
    }
}
