using System.Collections.Immutable;

namespace Common;

public class ExampleClass
{
    // primitives
    public int PrimitiveInt { get; set; }
    public string PrimitiveString { get; }

    // array
    public int[] Array { get; }

    // immutable array
    public ImmutableArray<int> ImmutableArr { get; }

    // List and IEnumerable
    public List<string> List { get; }
    public IEnumerable<string> Enumerable { get; }

    // immutable list
    public ImmutableList<string> ImmutableLst { get; }

    // dictionary and immutable dictionary
    public Dictionary<string, int> Dictionary { get; }
    public ImmutableDictionary<string, int> ImmutableDict { get; }

    // hashset and immutable hashset
    public HashSet<int> HashSet { get; }
    public ImmutableHashSet<int> ImmutableSet { get; }

    // nested other example
    public ExampleOtherClass Other { get; }

    
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
    }
}
