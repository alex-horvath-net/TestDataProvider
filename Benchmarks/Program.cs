using BenchmarkDotNet.Running;

namespace Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<GenerationBenchmarks>();
        //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
