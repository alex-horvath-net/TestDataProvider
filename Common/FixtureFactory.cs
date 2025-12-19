using System.Collections.Immutable;
using AutoFixture;

namespace Common; 
public class FixtureFactory {
    public static Fixture CreateByAutoFixture() {
        var fixture = new Fixture();
        fixture.RepeatCount = 3;
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());
        return fixture;
    }

    public static BogusFixture CreateByAutoBogus() {
        var fixture = new BogusFixture();
        fixture.RepeatCount = 3;
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
        fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
        fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());
        return fixture;
    }
}
   