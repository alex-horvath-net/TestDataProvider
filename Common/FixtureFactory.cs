using System.Collections.Immutable;
using AutoFixture;

namespace Common;

public static class FixtureFactory
{
    public static Fixture CreateByAutoFixture()
    {
        var fixture = new Fixture { RepeatCount = 3 };
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());
        return fixture;
    }

    public static BogusFixture CreateByAutoBogus()
    {
        var fixture = new BogusFixture { RepeatCount = 3 };

        // Immutable helpers
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());

        // Application-specific builders to avoid slow reflection paths
        fixture.Register(BuildExampleOtherClass(fixture));
        fixture.Register(BuildExampleClass(fixture));
        fixture.Register(BuildExampleRecord(fixture));

        return fixture;
    }

    private static Func<ExampleOtherClass> BuildExampleOtherClass(BogusFixture fixture) => () =>
    {
        var primitiveInt = SanitizeInt(fixture.Create<int>());
        var primitiveString = SanitizeString(fixture.Create<string>());

        var array = fixture.CreateMany<int>().ToArray();
        var immutableArray = array.ToImmutableArray();

        var list = fixture.CreateMany<string>().ToList();
        var enumerable = list.AsEnumerable();
        var immutableList = list.ToImmutableList();

        var dictionary = list.ToDictionary(x => x, x => x.Length);
        var immutableDict = dictionary.ToImmutableDictionary();

        var hashSet = fixture.CreateMany<int>().ToHashSet();
        var immutableSet = hashSet.ToImmutableHashSet();

        return new ExampleOtherClass(
            primitiveInt,
            primitiveString,
            array,
            immutableArray,
            list,
            enumerable,
            immutableList,
            dictionary,
            immutableDict,
            hashSet,
            immutableSet);
    };

    private static Func<ExampleClass> BuildExampleClass(BogusFixture fixture) => () =>
    {
        var primitiveInt = SanitizeInt(fixture.Create<int>());
        var primitiveString = SanitizeString(fixture.Create<string>());

        var array = fixture.CreateMany<int>().ToArray();
        var immutableArray = array.ToImmutableArray();

        var list = fixture.CreateMany<string>().ToList();
        var enumerable = list.AsEnumerable();
        var immutableList = list.ToImmutableList();

        var dictionary = list.ToDictionary(x => x, x => x.Length);
        var immutableDict = dictionary.ToImmutableDictionary();

        var hashSet = fixture.CreateMany<int>().ToHashSet();
        var immutableSet = hashSet.ToImmutableHashSet();

        var timestamp = fixture.Create<DateTime>();
        var other = fixture.Create<ExampleOtherClass>();

        return new ExampleClass(
            primitiveInt,
            primitiveString,
            array,
            immutableArray,
            list,
            enumerable,
            immutableList,
            dictionary,
            immutableDict,
            hashSet,
            immutableSet,
            timestamp,
            other);
    };

    private static Func<ExampleRecord> BuildExampleRecord(BogusFixture fixture) => () =>
    {
        var primitiveInt = SanitizeInt(fixture.Create<int>());
        var primitiveString = SanitizeString(fixture.Create<string>());

        var array = fixture.CreateMany<int>().ToArray();
        var immutableArray = array.ToImmutableArray();

        var list = fixture.CreateMany<string>().ToList();
        var enumerable = list.AsEnumerable();
        var immutableList = list.ToImmutableList();

        var dictionary = list.ToDictionary(x => x, x => x.Length);
        var immutableDict = dictionary.ToImmutableDictionary();

        var hashSet = fixture.CreateMany<int>().ToHashSet();
        var immutableSet = hashSet.ToImmutableHashSet();

        var other = fixture.Create<ExampleOtherClass>();

        return new ExampleRecord(
            primitiveInt,
            primitiveString,
            array,
            immutableArray,
            list,
            enumerable,
            immutableList,
            dictionary,
            immutableDict,
            hashSet,
            immutableSet,
            other);
    };

    private static int SanitizeInt(int value)
    {
        var v = Math.Abs(value);
        return v == 0 ? 1 : v;
    }

    private static string SanitizeString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? $"s{Guid.NewGuid():N}" : value;
    }
}