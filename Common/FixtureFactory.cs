using System.Collections.Immutable;
using Bogus;
using AutoFixture;

namespace Common;

public static class FixtureFactory {
    public static Fixture CreateByAutoFixture() {
        var fixture = new Fixture { RepeatCount = 3 };
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());
        return fixture;
    }

    public static BogusFixture CreateByBogus() {
        var fixture = new BogusFixture { RepeatCount = 3 };
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());

        return fixture;
    }
}

