using System.Collections.Immutable;
using AutoBogus;
using AutoFixture;
using Common;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class GenerationBenchmarks {
    private Fixture _autoFixture = null!;
    private BogusFixture _bogusFixture = null!;

    [GlobalSetup]
    public void Setup() {
        _autoFixture = new Fixture {
            RepeatCount = 3
        };

        _autoFixture.Register(() => _autoFixture.CreateMany<int>().ToImmutableArray());
        _autoFixture.Register(() => _autoFixture.CreateMany<string>().ToImmutableList());
        _autoFixture.Register(() => _autoFixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        _autoFixture.Register(() => _autoFixture.CreateMany<int>().ToImmutableHashSet());


        _bogusFixture = new BogusFixture { RepeatCount = 3 };

        _bogusFixture.Register(() => _bogusFixture.CreateMany<int>().ToImmutableArray());
        _bogusFixture.Register(() => _bogusFixture.CreateMany<string>().ToImmutableList());
        _bogusFixture.Register(() => _bogusFixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        _bogusFixture.Register(() => _bogusFixture.CreateMany<int>().ToImmutableHashSet());
    }

    [Benchmark]
    public ExampleClass AutoFixture_ExampleClass() => _autoFixture.Create<ExampleClass>();

    [Benchmark]
    public ExampleRecord AutoFixture_ExampleRecord() => _autoFixture.Create<ExampleRecord>();

    [Benchmark]
    public ExampleClass AutoBogus_ExampleClass() => AutoFaker.Generate<ExampleClass>(cfg => cfg.WithRepeatCount(3));

    [Benchmark]
    public ExampleRecord AutoBogus_ExampleRecord_WithImmutableFactories() {
        var fixture = new BogusFixture { RepeatCount = 3 };

        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());

        return fixture.Create<ExampleRecord>();
    }
}
