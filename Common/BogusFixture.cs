using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using Bogus;

namespace Common;

public sealed class BogusFixture
{
    private static readonly Type StringType = typeof(string);
    private static readonly Type IntType = typeof(int);
    private static readonly Type LongType = typeof(long);
    private static readonly Type ShortType = typeof(short);
    private static readonly Type ByteType = typeof(byte);
    private static readonly Type BoolType = typeof(bool);
    private static readonly Type DoubleType = typeof(double);
    private static readonly Type FloatType = typeof(float);
    private static readonly Type DecimalType = typeof(decimal);
    private static readonly Type GuidType = typeof(Guid);
    private static readonly Type DateTimeType = typeof(DateTime);
    private static readonly HashSet<Type> SimpleTypes = new()
    {
        StringType,
        DecimalType,
        DateTimeType,
        GuidType,
        ByteType,
        ShortType,
        IntType,
        LongType,
        FloatType,
        DoubleType
    };

    private static readonly Type GenericEnumerableType = typeof(IEnumerable<>);
    private static readonly Type GenericListType = typeof(List<>);
    private static readonly Type GenericHashSetType = typeof(HashSet<>);
    private static readonly Type GenericDictionaryType = typeof(Dictionary<,>);
    private static readonly Type GenericImmutableArrayType = typeof(ImmutableArray<>);
    private static readonly Type GenericImmutableListType = typeof(ImmutableList<>);
    private static readonly Type GenericImmutableHashSetType = typeof(ImmutableHashSet<>);
    private static readonly Type GenericImmutableDictionaryType = typeof(ImmutableDictionary<,>);
    private static readonly Type GenericKeyValuePairType = typeof(KeyValuePair<,>);
    private static readonly ConcurrentBag<HashSet<Type>> VisitedSets = new();

    private readonly ConcurrentDictionary<Type, Func<object>> _factories = new();
    private readonly ConcurrentDictionary<Type, Func<object>> _primitiveGenerators = new();
    private readonly ConcurrentDictionary<Type, ConstructorInfo?> _preferredConstructors = new();
    private readonly ConcurrentDictionary<Type, Array> _enumValuesCache = new();
    private readonly ThreadLocal<Randomizer> _random;
    private readonly ThreadLocal<HashSet<Type>> _creatingTypes = new(() => new HashSet<Type>());

    private static readonly MethodInfo ToImmutableArray = typeof(ImmutableArray)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(ImmutableArray.ToImmutableArray)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == GenericEnumerableType);

    private static readonly MethodInfo ToImmutableList = typeof(ImmutableList)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(ImmutableList.ToImmutableList)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == GenericEnumerableType);

    private static readonly MethodInfo ToImmutableHashSet = typeof(ImmutableHashSet)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(ImmutableHashSet.ToImmutableHashSet)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == GenericEnumerableType);

    private static readonly MethodInfo CreateRangeTemplate = typeof(ImmutableDictionary)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(ImmutableDictionary.CreateRange)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == GenericEnumerableType);

    private static readonly ConcurrentDictionary<Type, Func<IList, object>> ImmutableArrayConverters = new();
    private static readonly ConcurrentDictionary<Type, Func<IList, object>> ImmutableListConverters = new();
    private static readonly ConcurrentDictionary<Type, Func<IList, object>> ImmutableHashSetConverters = new();
    private static readonly ConcurrentDictionary<(Type key, Type value), Func<IList, object>> ImmutableDictionaryConverters = new();

    public int RepeatCount { get; set; } = 3;

    public void Register<T>(Func<T> factory) => _factories[typeof(T)] = () => factory()!;

    public T Create<T>()
    {
        var visited = RentVisitedSet();
        try
        {
            return (T)Create(typeof(T), visited);
        }
        finally
        {
            ReturnVisitedSet(visited);
        }
    }

    public IEnumerable<T> CreateMany<T>(int? count = null)
    {
        var total = count ?? RepeatCount;
        for (var i = 0; i < total; i++)
            yield return Create<T>();
    }

    private object Create(Type type, HashSet<Type> visitedTypes)
    {
        if (TryCreateInstanceByFactory(type, visitedTypes, out var instanceByFactory))
            return instanceByFactory ?? GeneratePrimitive(type);

        if (visitedTypes.Contains(type))
            return GeneratePrimitive(type);

        if (TryCreateGeneric(type, visitedTypes, out var genericResult))
            return genericResult ?? GeneratePrimitive(type);

        if (type.IsArray)
            return CreateArray(type, visitedTypes);

        if (IsSimple(type))
            return GeneratePrimitive(type);

        return CreateUsingConstructor(type, visitedTypes);
    }

    private bool TryCreateInstanceByFactory(Type type, HashSet<Type> createdTypes, out object? result)
    {
        result = null;
        if (!_factories.TryGetValue(type, out var factory))
            return false;

        var creatingTypes = _creatingTypes.Value!;
        if (creatingTypes.Contains(type))
            return false;

        creatingTypes.Add(type);
        createdTypes.Add(type);
        try
        {
            result = factory();
            return true;
        }
        finally
        {
            createdTypes.Remove(type);
            creatingTypes.Remove(type);
        }
    }

    private bool TryCreateGeneric(Type type, HashSet<Type> visitedTypes, out object? result)
    {
        result = null;
        if (!type.IsGenericType)
            return false;

        var genDef = type.GetGenericTypeDefinition();
        var genArgs = type.GetGenericArguments();

        if (genDef == GenericImmutableArrayType)
        {
            var list = BuildList(genArgs[0], RepeatCount, visitedTypes);
            var converter = ImmutableArrayConverters.GetOrAdd(genArgs[0], t => CreateImmutableConverter(ToImmutableArray, t));
            result = converter(list);
            return true;
        }

        if (genDef == GenericImmutableListType)
        {
            var list = BuildList(genArgs[0], RepeatCount, visitedTypes);
            var converter = ImmutableListConverters.GetOrAdd(genArgs[0], t => CreateImmutableConverter(ToImmutableList, t));
            result = converter(list);
            return true;
        }

        if (genDef == GenericImmutableHashSetType)
        {
            var set = BuildHashSet(genArgs[0], RepeatCount, visitedTypes);
            var converter = ImmutableHashSetConverters.GetOrAdd(genArgs[0], t => CreateImmutableConverter(ToImmutableHashSet, t));
            result = converter((IList)set);
            return true;
        }

        if (genDef == GenericImmutableDictionaryType)
        {
            result = BuildImmutableDictionary(genArgs[0], genArgs[1], visitedTypes);
            return true;
        }

        if (genDef == GenericEnumerableType)
        {
            result = BuildList(genArgs[0], RepeatCount, visitedTypes);
            return true;
        }

        if (genDef == GenericListType)
        {
            result = BuildList(genArgs[0], RepeatCount, visitedTypes);
            return true;
        }

        if (genDef == GenericHashSetType)
        {
            result = BuildHashSet(genArgs[0], RepeatCount, visitedTypes);
            return true;
        }

        if (genDef == GenericDictionaryType)
        {
            result = BuildDictionary(genArgs[0], genArgs[1], RepeatCount, visitedTypes);
            return true;
        }

        return false;
    }

    private object CreateArray(Type type, HashSet<Type> visitedTypes)
    {
        var elem = type.GetElementType()!;
        var arr = Array.CreateInstance(elem, RepeatCount);
        visitedTypes.Add(type);
        for (var i = 0; i < RepeatCount; i++)
            arr.SetValue(Create(elem, visitedTypes), i);
        visitedTypes.Remove(type);
        return arr;
    }

    private object CreateUsingConstructor(Type type, HashSet<Type> visitedTypes)
    {
        var ctors = type.GetConstructors();
        if (ctors.Length == 0)
        {
            try { return Activator.CreateInstance(type)!; } catch { return GeneratePrimitive(type); }
        }

        var ctor = _preferredConstructors.GetOrAdd(type, t =>
        {
            var c = t.GetConstructors();
            return c.Length == 0
                ? null
                : c.OrderByDescending(p => p.GetParameters().Length).First();
        });

        if (ctor is null)
        {
            try { return Activator.CreateInstance(type)!; } catch { return GeneratePrimitive(type); }
        }

        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        visitedTypes.Add(type);
        try
        {
            for (var i = 0; i < parameters.Length; i++)
                args[i] = Create(parameters[i].ParameterType, visitedTypes);

            for (var i = 0; i < parameters.Length; i++)
                args[i] ??= GeneratePrimitive(parameters[i].ParameterType);

            return ctor.Invoke(args);
        }
        finally
        {
            visitedTypes.Remove(type);
        }
    }

    private Func<IList, object> CreateImmutableConverter(MethodInfo openGenericMethod, Type elementType)
    {
        var closed = openGenericMethod.MakeGenericMethod(elementType);
        return list => closed.Invoke(null, new object[] { list })!;
    }

    private IList BuildList(Type elementType, int count, HashSet<Type> visitedTypes)
    {
        var list = (IList)Activator.CreateInstance(GenericListType.MakeGenericType(elementType))!;
        for (var i = 0; i < count; i++)
            list.Add(Create(elementType, visitedTypes));
        return list;
    }

    private object BuildHashSet(Type elementType, int count, HashSet<Type> visitedTypes)
    {
        var setType = GenericHashSetType.MakeGenericType(elementType);
        var set = Activator.CreateInstance(setType)!;
        var add = setType.GetMethod("Add", new[] { elementType })!;
        var items = BuildList(elementType, count * 3, visitedTypes); // extra to reduce duplicates
        var added = 0;
        foreach (var item in items)
        {
            var addedResult = add.Invoke(set, new[] { item });
            if (addedResult is bool ok && ok)
                added++;
            if (added >= count)
                break;
        }
        return set;
    }

    private object BuildImmutableDictionary(Type keyType, Type valueType, HashSet<Type> visitedTypes)
    {
        return BuildImmutableDictionary(keyType, valueType, RepeatCount, visitedTypes);
    }

    private object BuildImmutableDictionary(Type keyType, Type valueType, int count, HashSet<Type> visitedTypes)
    {
        var kvpType = GenericKeyValuePairType.MakeGenericType(keyType, valueType);
        var listType = GenericListType.MakeGenericType(kvpType);
        var list = (IList)Activator.CreateInstance(listType)!;

        var keys = BuildList(keyType, count * 3, visitedTypes).Cast<object?>().Distinct().Take(count).ToArray();
        var values = BuildList(valueType, count * 2, visitedTypes).Cast<object?>().ToArray();
        var pairCount = Math.Min(keys.Length, values.Length);
        var ctor = kvpType.GetConstructor(new[] { keyType, valueType })!;
        for (var i = 0; i < pairCount; i++)
        {
            var key = keys[i] ?? GeneratePrimitive(keyType);
            var val = values[i] ?? GeneratePrimitive(valueType);
            list.Add(ctor.Invoke(new[] { key, val }));
        }

        var converter = ImmutableDictionaryConverters.GetOrAdd((keyType, valueType), kv =>
        {
            var closed = CreateRangeTemplate.MakeGenericMethod(kv.key, kv.value);
            return kvpList => closed.Invoke(null, new object[] { kvpList })!;
        });

        return converter(list);
    }

    private object BuildDictionary(Type keyType, Type valueType, int count, HashSet<Type> visitedTypes)
    {
        var dictType = GenericDictionaryType.MakeGenericType(keyType, valueType);
        var dict = (IDictionary)Activator.CreateInstance(dictType)!;
        var add = dictType.GetMethod("Add", new[] { keyType, valueType })!;

        var keys = BuildList(keyType, count * 2, visitedTypes).Cast<object?>().Distinct().Take(count).ToArray();
        var values = BuildList(valueType, count * 2, visitedTypes).Cast<object?>().ToArray();
        var pairCount = Math.Min(keys.Length, values.Length);
        for (var i = 0; i < pairCount; i++)
        {
            var key = keys[i] ?? GeneratePrimitive(keyType);
            var val = values[i] ?? GeneratePrimitive(valueType);
            add.Invoke(dict, new[] { key, val });
        }
        return dict;
    }

    private object GeneratePrimitive(Type type)
    {
        var generator = _primitiveGenerators.GetOrAdd(type, CreatePrimitiveGenerator);
        return generator();
    }

    private Func<object> CreatePrimitiveGenerator(Type type)
    {
        Randomizer Rng() => _random.Value!;

        if (type == StringType) return () => Rng().AlphaNumeric(8);
        if (type == IntType) return () => Math.Max(1, Rng().Int(1, int.MaxValue));
        if (type == LongType) return () => Math.Abs(Rng().Long()) + 1;
        if (type == ShortType) return () => (short)Rng().Int(short.MinValue, short.MaxValue);
        if (type == ByteType) return () => Rng().Byte();
        if (type == BoolType) return () => Rng().Bool();
        if (type == DoubleType) return () => Rng().Double();
        if (type == FloatType) return () => (float)Rng().Double();
        if (type == DecimalType) return () => Rng().Decimal();
        if (type == GuidType) return () => Guid.NewGuid();
        if (type == DateTimeType) return () => DateTime.UtcNow.AddMilliseconds(Rng().Int(-100000, 100000));
        if (type.IsEnum)
        {
            var values = _enumValuesCache.GetOrAdd(type, Enum.GetValues);
            return () => values.GetValue(Rng().Int(0, values.Length - 1))!;
        }

        return () => Activator.CreateInstance(type) ?? string.Empty;
    }

    private static bool IsSimple(Type type) =>
        type.IsPrimitive || type.IsEnum || SimpleTypes.Contains(type);

    private static HashSet<Type> RentVisitedSet() => VisitedSets.TryTake(out var set) ? set : new HashSet<Type>();

    private static void ReturnVisitedSet(HashSet<Type> set)
    {
        set.Clear();
        VisitedSets.Add(set);
    }

    public BogusFixture(int? seed = null)
    {
        _random = new ThreadLocal<Randomizer>(() => seed.HasValue ? new Randomizer(seed.Value) : new Randomizer());
    }

    public BogusFixture() : this(null) { }

    public BogusFixtureBuilder<T> Build<T>() => new(this);

}
