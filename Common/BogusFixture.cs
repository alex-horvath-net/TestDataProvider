using System.Collections.Concurrent;
using System.Collections.Immutable;
using AutoBogus;

namespace Common;

public class BogusFixture {
    private readonly ConcurrentDictionary<Type, Func<object>> registeredFactories = new();
    private static readonly ConcurrentDictionary<Type, Func<object>> autoFakerGenerators = new();

    public int RepeatCount { get; set; } = 3;

    public void Register<T>(Func<T> factory) => registeredFactories[typeof(T)] = () => factory();

    public T Create<T>() => (T)Create(typeof(T), new HashSet<Type>());

    public IEnumerable<T> CreateMany<T>(int? count = null) => AutoFaker.Generate<T>(count ?? RepeatCount, cfg => cfg.WithRepeatCount(RepeatCount));

    private object Create(Type type, HashSet<Type> visited) {
        if (registeredFactories.TryGetValue(type, out var factory))
            return factory()!;

        if (visited.Contains(type))
            return AutoFakerGenerate(type);

        if (type.IsGenericType) {
            var genDef = type.GetGenericTypeDefinition();
            var genArgs = type.GetGenericArguments();

            if (genDef == typeof(ImmutableArray<>))
                return InvokeGenericHelper(nameof(CreateImmutableArrayGeneric), genArgs[0]);

            if (genDef == typeof(ImmutableList<>))
                return InvokeGenericHelper(nameof(CreateImmutableListGeneric), genArgs[0]);

            if (genDef == typeof(ImmutableHashSet<>))
                return InvokeGenericHelper(nameof(CreateImmutableHashSetGeneric), genArgs[0]);

            if (genDef == typeof(ImmutableDictionary<,>))
                return InvokeGenericHelper(nameof(CreateImmutableDictionaryGeneric), genArgs[0], genArgs[1]);

            if (genDef == typeof(IEnumerable<>))
                return InvokeGenericHelper(nameof(CreateEnumerableGeneric), genArgs[0]);

            if (genDef == typeof(List<>))
                return InvokeGenericHelper(nameof(CreateListGeneric), genArgs[0]);

            if (genDef == typeof(HashSet<>))
                return InvokeGenericHelper(nameof(CreateHashSetGeneric), genArgs[0]);

            if (genDef == typeof(Dictionary<,>))
                return InvokeGenericHelper(nameof(CreateDictionaryGeneric), genArgs[0], genArgs[1]);
        }

        if (type.IsArray) {
            var elem = type.GetElementType()!;
            var arr = Array.CreateInstance(elem, RepeatCount);
            visited.Add(type);
            for (var i = 0; i < RepeatCount; i++)
                arr.SetValue(Create(elem, visited), i);
            visited.Remove(type);
            return arr;
        }

        if (type.IsPrimitive || type.IsEnum || type == typeof(decimal))
            return AutoFakerGenerate(type);

        if (type == typeof(string)) {
            var s = (string)AutoFakerGenerate(type)!;
            return string.IsNullOrWhiteSpace(s) ? $"s{Guid.NewGuid():N}" : s;
        }

        if (type == typeof(int)) {
            var v = Math.Abs((int)AutoFakerGenerate(type)!);
            return v == 0 ? 1 : v;
        }

        var ctors = type.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (ctors.Length == 0) {
            try { return Activator.CreateInstance(type)!; } catch { return AutoFakerGenerate(type); }
        }

        var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        visited.Add(type);
        try {
            for (var i = 0; i < parameters.Length; i++)
                args[i] = Create(parameters[i].ParameterType, visited);

            for (var i = 0; i < parameters.Length; i++)
                args[i] ??= AutoFakerGenerate(parameters[i].ParameterType);

            return ctor.Invoke(args);
        }
        catch {
            return AutoFakerGenerate(type);
        }
        finally {
            visited.Remove(type);
        }
    }

    private object AutoFakerGenerate(Type type) {
        var generator = autoFakerGenerators.GetOrAdd(type, t => {
            var method = typeof(AutoFaker)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == nameof(AutoFaker.Generate) && m.IsGenericMethodDefinition && m.GetParameters().Length <= 1)
                .MakeGenericMethod(t);

            return () => {
                var parameters = method.GetParameters().Length == 0
                    ? Array.Empty<object?>()
                    : new object?[] { null };
                return method.Invoke(null, parameters)!;
            };
        });

        return generator();
    }

    private object InvokeGenericHelper(string methodName, params Type[] typeArgs) {
        var method = typeof(BogusFixture).GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var closed = method.MakeGenericMethod(typeArgs);
        return closed.Invoke(this, null)!;
    }

    private ImmutableArray<T> CreateImmutableArrayGeneric<T>() => BuildList<T>(RepeatCount).ToImmutableArray();

    private ImmutableList<T> CreateImmutableListGeneric<T>() => BuildList<T>(RepeatCount).ToImmutableList();

    private ImmutableHashSet<T> CreateImmutableHashSetGeneric<T>() => BuildHashSet<T>(RepeatCount * 3).ToImmutableHashSet();

    private ImmutableDictionary<TKey, TValue> CreateImmutableDictionaryGeneric<TKey, TValue>() where TKey : notnull => BuildDictionary<TKey, TValue>(RepeatCount).ToImmutableDictionary();

    private IEnumerable<T> CreateEnumerableGeneric<T>() => BuildList<T>(RepeatCount);

    private List<T> CreateListGeneric<T>() => BuildList<T>(RepeatCount);

    private HashSet<T> CreateHashSetGeneric<T>() => BuildHashSet<T>(RepeatCount);

    private Dictionary<TKey, TValue> CreateDictionaryGeneric<TKey, TValue>() where TKey : notnull => BuildDictionary<TKey, TValue>(RepeatCount);

    private List<T> BuildList<T>(int count) => CreateMany<T>(count).ToList();

    private HashSet<T> BuildHashSet<T>(int count) => new(CreateMany<T>(count));

    private Dictionary<TKey, TValue> BuildDictionary<TKey, TValue>(int count) where TKey : notnull {
        var keys = CreateMany<TKey>(count * 2).Distinct().Take(count).ToArray();
        var values = CreateMany<TValue>(count * 2).ToArray();
        var dict = new Dictionary<TKey, TValue>();
        var pairCount = Math.Min(keys.Length, values.Length);
        for (var i = 0; i < pairCount; i++)
            dict[keys[i]] = values[i];
        return dict;
    }
}


