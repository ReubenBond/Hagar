using System;
using BenchmarkDotNet.Configs;

namespace Benchmarks.Utilities
{
    public class PayloadSizeColumnAttribute : Attribute, IConfigSource
    {
        public PayloadSizeColumnAttribute(string columnName = "Payload")
        {
            this.Config = ManualConfig.CreateEmpty().With(
                new MethodResultColumn(columnName,
                    val =>
                    {
                        uint result;
                        switch (val)
                        {
                            case int i:
                                result = (uint) i;
                                break;
                            case uint i:
                                result = i;
                                break;
                            case long i:
                                result = (uint) i;
                                break;
                            case ulong i:
                                result = (uint) i;
                                break;
                            default: return "Invalid";
                        }

                        return result + " B";
                    }));
        }

        public IConfig Config { get; }
    }
}