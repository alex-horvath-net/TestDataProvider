using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;

namespace Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        var config =  DefaultConfig.Instance
            .AddExporter(MarkdownExporter.Console)
            .AddExporter(AsciiDocExporter.Default)
            .AddLogger(ConsoleLogger.Default);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
