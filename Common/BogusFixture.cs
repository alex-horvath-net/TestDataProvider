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

    private readonly ConcurrentDictionary<Type, Func<object>> _registeredFactories = new();
    private readonly ConcurrentDictionary<Type, Func<object>> _primitiveGenerators = new();
    private readonly ConcurrentDictionary<Type, ConstructorInfo?> _preferredConstructors = new();
    private readonly Randomizer _random = new();

    private static readonly MethodInfo ImmutableArrayToImmutableArray = typeof(ImmutableArray)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(ImmutableArray.ToImmutableArray)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == GenericEnumerableType);

    private static readonly MethodInfo ImmutableListToImmutableList = typeof(ImmutableList)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(ImmutableList.ToImmutableList)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == GenericEnumerableType);

    private static readonly MethodInfo ImmutableHashSetToImmutableHashSet = typeof(ImmutableHashSet)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(ImmutableHashSet.ToImmutableHashSet)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsGenericType
                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == GenericEnumerableType);

    private static readonly MethodInfo ImmutableDictionaryCreateRangeTemplate = typeof(ImmutableDictionary)
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

    public void Register<T>(Func<T> factory) => _registeredFactories[typeof(T)] = () => factory()!;

    public T Create<T>() => (T)Create(typeof(T), new HashSet<Type>());

    public IEnumerable<T> CreateMany<T>(int? count = null)
    {
        var total = count ?? RepeatCount;
        for (var i = 0; i < total; i++)
            yield return Create<T>();
    }

    private object Create(Type type, HashSet<Type> visitedTypes)
    {
        if (_registeredFactories.TryGetValue(type, out var factory))
            return factory();

        if (visitedTypes.Contains(type))
            return GeneratePrimitive(type);

        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            var genArgs = type.GetGenericArguments();

            if (genDef == GenericImmutableArrayType)
            {
                var list = BuildList(genArgs[0], RepeatCount, visitedTypes);
                var converter = ImmutableArrayConverters.GetOrAdd(genArgs[0], t => CreateImmutableConverter(ImmutableArrayToImmutableArray, t));
                return converter(list);
            }

            if (genDef == GenericImmutableListType)
            {
                var list = BuildList(genArgs[0], RepeatCount, visitedTypes);
                var converter = ImmutableListConverters.GetOrAdd(genArgs[0], t => CreateImmutableConverter(ImmutableListToImmutableList, t));
                return converter(list);
            }

            if (genDef == GenericImmutableHashSetType)
            {
                var set = BuildHashSet(genArgs[0], RepeatCount, visitedTypes);
                var converter = ImmutableHashSetConverters.GetOrAdd(genArgs[0], t => CreateImmutableConverter(ImmutableHashSetToImmutableHashSet, t));
                return converter((IList)set);
            }

            if (genDef == GenericImmutableDictionaryType)
                return BuildImmutableDictionary(genArgs[0], genArgs[1], visitedTypes);

            if (genDef == GenericEnumerableType)
                return BuildList(genArgs[0], RepeatCount, visitedTypes);

            if (genDef == GenericListType)
                return BuildList(genArgs[0], RepeatCount, visitedTypes);

            if (genDef == GenericHashSetType)
                return BuildHashSet(genArgs[0], RepeatCount, visitedTypes);

            if (genDef == GenericDictionaryType)
                return BuildDictionary(genArgs[0], genArgs[1], RepeatCount, visitedTypes);
        }

        if (type.IsArray)
        {
            var elem = type.GetElementType()!;
            var arr = Array.CreateInstance(elem, RepeatCount);
            visitedTypes.Add(type);
            for (var i = 0; i < RepeatCount; i++)
                arr.SetValue(Create(elem, visitedTypes), i);
            visitedTypes.Remove(type);
            return arr;
        }

        if (IsSimple(type))
            return GeneratePrimitive(type);

        var ctors = type.GetConstructors();
        if (ctors.Length == 0)
        {
            try { return Activator.CreateInstance(type)!; } catch { return GeneratePrimitive(type); }
        }

        var ctor = _preferredConstructors.GetOrAdd(type, t =>
        {
            var ctors = t.GetConstructors();
            return ctors.Length == 0
                ? null
                : ctors.OrderByDescending(c => c.GetParameters().Length).First();
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
            var closed = ImmutableDictionaryCreateRangeTemplate.MakeGenericMethod(kv.key, kv.value);
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
        if (type == StringType) return () => _random.AlphaNumeric(8);
        if (type == IntType) return () => Math.Max(1, _random.Int(1, int.MaxValue));
        if (type == LongType) return () => Math.Abs(_random.Long()) + 1;
        if (type == ShortType) return () => (short)_random.Int(short.MinValue, short.MaxValue);
        if (type == ByteType) return () => _random.Byte();
        if (type == BoolType) return () => _random.Bool();
        if (type == DoubleType) return () => _random.Double();
        if (type == FloatType) return () => (float)_random.Double();
        if (type == DecimalType) return () => _random.Decimal();
        if (type == GuidType) return () => Guid.NewGuid();
        if (type == DateTimeType) return () => DateTime.UtcNow.AddMilliseconds(_random.Int(-100000, 100000));
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return () => values.GetValue(_random.Int(0, values.Length - 1))!;
        }

        return () => Activator.CreateInstance(type) ?? string.Empty;
    }

    private static bool IsSimple(Type type) =>
        type.IsPrimitive || type.IsEnum || SimpleTypes.Contains(type);
}
