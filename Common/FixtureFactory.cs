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
        //fixture.Register(() => fixture.Create<ExampleOtherClass>());
        return fixture;
    }

    public static BogusFixture CreateByBogus() {
        var fixture = new BogusFixture { RepeatCount = 3 };
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());
        fixture.Register(() => fixture.Create<ExampleOtherClass>());
        fixture.Register(() =>
        {
            var temp = new BogusFixture { RepeatCount = fixture.RepeatCount };
            temp.Register(() => temp.CreateMany<int>().ToImmutableArray());
            temp.Register(() => temp.CreateMany<string>().ToImmutableList());
            temp.Register(() => temp.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
            temp.Register(() => temp.CreateMany<int>().ToImmutableHashSet());
            temp.Register(() => temp.Create<ExampleOtherClass>());

            var instance = temp.Create<ExampleClass>();
            instance.PrimitiveInt = 42;
            return instance;
        });


        return fixture;
    }
}

