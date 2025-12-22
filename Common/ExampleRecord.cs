using System.Collections.Immutable;

namespace Common;

public record ExampleRecord(
    int PrimitiveInt,
    string PrimitiveString,
    int[] Array,
    ImmutableArray<int> ImmutableArr,
    List<string> List,
    IEnumerable<string> Enumerable,
    ImmutableList<string> ImmutableLst,
    Dictionary<string, int> Dictionary,
    ImmutableDictionary<string, int> ImmutableDict,
    HashSet<int> HashSet,
    ImmutableHashSet<int> ImmutableSet,
    ExampleOtherClass OtherClass,
    ExampleOtherRecord OtherRecord,
    DateTimeOffset Timestamp
    );
