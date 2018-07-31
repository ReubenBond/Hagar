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
                var benchmarks = new ComplexTypeBenchmarks();
                while (true)
                {
                    benchmarks.SerializeComplex();
                }
            }

            if (args.Length > 0 && args[0] == "structloop")
            {
                var benchmarks = new ComplexTypeBenchmarks();
                while (true)
                {
                    benchmarks.SerializeStruct();
                }
            }

            var switcher = new BenchmarkSwitcher(new[]
            {
                typeof(DeserializeBenchmark),
                typeof(SerializeBenchmark),
                typeof(StructSerializeBenchmark),
                typeof(StructDeserializeBenchmark),
                typeof(ComplexTypeBenchmarks)
            });

            switcher.Run(args);
        }
    }
}
