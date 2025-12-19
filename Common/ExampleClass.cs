using System;
using System.Collections.Immutable;

namespace Common;

public class ExampleClass
{
    public int PrimitiveInt { get; set; }
    public string PrimitiveString { get; }

    public int[] Array { get; }

    public ImmutableArray<int> ImmutableArr { get; }

    public List<string> List { get; }
    public IEnumerable<string> Enumerable { get; }

    public ImmutableList<string> ImmutableLst { get; }

    public Dictionary<string, int> Dictionary { get; }
    public ImmutableDictionary<string, int> ImmutableDict { get; }

    public HashSet<int> HashSet { get; }
    public ImmutableHashSet<int> ImmutableSet { get; }

    public ExampleOtherClass Other { get; }

    public DateTime Timestamp { get; }

    public ExampleClass(
        int primitiveInt,
        string primitiveString,
        int[] array,
        ImmutableArray<int> immutableArray,
        List<string> list,
        IEnumerable<string> enumerable,
        ImmutableList<string> immutableList,
        Dictionary<string, int> dictionary,
        ImmutableDictionary<string, int> immutableDictionary,
        HashSet<int> hashSet,
        ImmutableHashSet<int> immutableHashSet,
        DateTime timestamp,
        ExampleOtherClass other)
    {
        PrimitiveInt = primitiveInt;
        PrimitiveString = primitiveString;
        Array = array;
        ImmutableArr = immutableArray;
        List = list;
        Enumerable = enumerable;
        ImmutableLst = immutableList;
        Dictionary = dictionary;
        ImmutableDict = immutableDictionary;
        HashSet = hashSet;
        ImmutableSet = immutableHashSet;
        Other = other;
        Timestamp = timestamp;
    }
}
