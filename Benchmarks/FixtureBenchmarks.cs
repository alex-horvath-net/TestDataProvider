using AutoFixture;
using BenchmarkDotNet.Attributes;
using Common;

namespace Benchmarks;

[MemoryDiagnoser]
public class FixtureBenchmarks {
    private Fixture _autoFixture = null!;
    private BogusFixture _bogus = null!;

    [GlobalSetup]
    public void Setup() {
        _autoFixture = FixtureFactory.CreateByAutoFixture();
        _bogus = FixtureFactory.CreateByBogus();
    }

    [Benchmark]
    public ExampleClass AutoFixture_ExampleClass() => _autoFixture.Create<ExampleClass>();

    [Benchmark]
    public ExampleRecord AutoFixture_ExampleRecord() => _autoFixture.Create<ExampleRecord>();

    [Benchmark]
    public ExampleClass Bogus_ExampleClass() => _bogus.Create<ExampleClass>();

    [Benchmark]
    public ExampleRecord Bogus_ExampleRecord() => _bogus.Create<ExampleRecord>();
}

