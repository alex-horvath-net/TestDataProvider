using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using AutoBogus;

namespace AutoBogusTest {
    public class BogusFixture {
        // Registered factories optimized for concurrent access
        private readonly ConcurrentDictionary<Type, Func<object>> registeredFactories = new();
        public int RepeatCount { get; set; }

        // cache commonly used reflection MethodInfos to avoid repeated lookups
        private static readonly MethodInfo ImmutableArrayCreateRange = GetCreateRangeMethod(typeof(ImmutableArray));
        private static readonly MethodInfo ImmutableListCreateRange = GetCreateRangeMethod(typeof(ImmutableList));
        private static readonly MethodInfo ImmutableDictionaryCreateRange = GetCreateRangeMethod(typeof(ImmutableDictionary));
        private static readonly MethodInfo ImmutableHashSetCreateRange = GetCreateRangeMethod(typeof(ImmutableHashSet));

        // compiled delegate caches to avoid MethodInfo.Invoke overhead
        private static readonly ConcurrentDictionary<Type, Func<object, object>> ImmutableArrayCreateRangeCache = new();
        private static readonly ConcurrentDictionary<Type, Func<object, object>> ImmutableListCreateRangeCache = new();
        private static readonly ConcurrentDictionary<(Type key, Type value), Func<object, object>> ImmutableDictionaryCreateRangeCache = new();
        private static readonly ConcurrentDictionary<Type, Func<object, object>> ImmutableHashSetCreateRangeCache = new();

        // cache for AutoFaker closed-generic Generate delegates
        private static readonly ConcurrentDictionary<Type, Func<object>> AutoFakerGenerateCache = new();

        private static readonly MethodInfo? AutoFakerGenerateNoParam;
        private static readonly MethodInfo? AutoFakerGenerateWithParam;

        static BogusFixture() {
            var generateMethods = typeof(AutoFaker)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Generate" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1)
                .ToArray();

            AutoFakerGenerateNoParam = generateMethods.FirstOrDefault(m => m.GetParameters().Length == 0);
            AutoFakerGenerateWithParam = generateMethods.FirstOrDefault(m => m.GetParameters().Length == 1);
        }

        private static MethodInfo GetCreateRangeMethod(Type collectionHelper) {
            // select CreateRange<T>(IEnumerable<T> items) style method
            var methods = collectionHelper.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "CreateRange" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);

            foreach (var m in methods) {
                var p = m.GetParameters()[0].ParameterType;
                if (p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return m;
            }
            // fallback to first CreateRange if not found
            return methods.First();
        }

        public BogusFixture() {
            // lightweight ctor: avoid global AutoFaker.Configure and instance creation so many fixtures are cheap
            RepeatCount = 3;
        }


        public void Register<T>(Func<T> factory) {
            registeredFactories[typeof(T)] = () => factory();
        }

        public T Create<T>() =>
            (T)Create(typeof(T), new HashSet<Type>());

        public IEnumerable<T> CreateMany<T>(int? count = null) =>
            AutoFaker.Generate<T>(count ?? RepeatCount);

        private bool TryGetFactory(Type t, out Func<object>? factory) => registeredFactories.TryGetValue(t, out factory);

        private object Create(Type type, HashSet<Type> visitedTypes) {
            if (TryGetFactory(type, out var factory))
            {
                return factory()!;
            }

            // guard recursion
            if (visitedTypes.Contains(type))
            {
                return AutoFakerGenerate(type);
            }

            // capture RepeatCount locally
            var repeat = RepeatCount;

            // handle generic types (both immutable helpers and common generic collections)
            if (type.IsGenericType) {
                var genDef = type.GetGenericTypeDefinition();
                var genArgs = type.GetGenericArguments();

                // Immutable collections
                if (genDef == typeof(ImmutableArray<>))
                {
                    var elemType = genArgs[0];
                    var list = CreateListInstance(elemType, repeat, visitedTypes);
                    var del = ImmutableArrayCreateRangeCache.GetOrAdd(elemType, t => CompileCreateRangeDelegate(ImmutableArrayCreateRange, t));
                    return del(list);
                }
                if (genDef == typeof(ImmutableList<>))
                {
                    var elemType = genArgs[0];
                    var list = CreateListInstance(elemType, repeat, visitedTypes);
                    var del = ImmutableListCreateRangeCache.GetOrAdd(elemType, t => CompileCreateRangeDelegate(ImmutableListCreateRange, t));
                    return del(list);
                }
                if (genDef == typeof(ImmutableDictionary<,>))
                {
                    var keyType = genArgs[0];
                    var valType = genArgs[1];
                    var dict = CreateDictionaryInstance(keyType, valType, repeat, visitedTypes);
                    var del = ImmutableDictionaryCreateRangeCache.GetOrAdd((keyType, valType), kv => CompileCreateRangeDelegate(ImmutableDictionaryCreateRange, kv.key, kv.value));
                    return del(dict);
                }
                if (genDef == typeof(ImmutableHashSet<>))
                {
                    var elemType = genArgs[0];
                    var list = CreateListInstance(elemType, repeat, visitedTypes);
                    var del = ImmutableHashSetCreateRangeCache.GetOrAdd(elemType, t => CompileCreateRangeDelegate(ImmutableHashSetCreateRange, t));
                    return del(list);
                }

                // common generic collections: IEnumerable<T>, List<T>, HashSet<T>, Dictionary<K,V>
                if (genDef == typeof(IEnumerable<>))
                {
                    var elemType = genArgs[0];
                    return CreateListInstance(elemType, repeat, visitedTypes);
                }
                if (genDef == typeof(List<>))
                {
                    var elemType = genArgs[0];
                    var list = (IList)Activator.CreateInstance(type)!;
                    for (var i = 0; i < repeat; i++)
                    {
                        list.Add(Create(elemType, visitedTypes));
                    }
                    return list;
                }
                if (genDef == typeof(HashSet<>))
                {
                    var elemType = genArgs[0];
                    return CreateHashSetInstance(elemType, repeat, visitedTypes);
                }
                if (genDef == typeof(Dictionary<,>))
                {
                    var keyType = genArgs[0];
                    var valType = genArgs[1];
                    return CreateDictionaryInstance(keyType, valType, repeat, visitedTypes);
                }
            }

            // handle arrays
            if (type.IsArray)
            {
                var elemType = type.GetElementType()!;
                var arr = Array.CreateInstance(elemType, RepeatCount);
                for (var i = 0; i < RepeatCount; i++)
                {
                    arr.SetValue(Create(elemType, visitedTypes), i);
                }
                return arr;
            }

            // primitive handling with small sanitization for tests using cached AutoFaker delegates
            if (type == typeof(string))
            {
                var s = (string)AutoFakerGenerate(typeof(string))!;
                if (string.IsNullOrWhiteSpace(s))
                {
                    s = "s" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
                }
                return s;
            }
            if (type == typeof(int))
            {
                var v = (int)AutoFakerGenerate(typeof(int))!;
                if (v == int.MinValue)
                {
                    v = 1;
                }
                v = Math.Abs(v);
                if (v == 0)
                {
                    v = 1;
                }
                return v;
            }
            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal))
                return AutoFakerGenerate(type);

            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (ctors.Length == 0)
            {
                try
                {
                    return Activator.CreateInstance(type)!;
                }
                catch
                {
                    return AutoFakerGenerate(type);
                }
            }

            var ctor = System.Linq.Enumerable.OrderByDescending(ctors, c => c.GetParameters().Length).First();
            var ctorParameterTypes = ctor.GetParameters();
            var ctorArguments = new object[ctorParameterTypes.Length];
            visitedTypes.Add(type);
            try
            {
                for (var i = 0; i < ctorParameterTypes.Length; i++)
                {
                    var ctorParameterType = ctorParameterTypes[i].ParameterType;
                    ctorArguments[i] = TryGetFactory(ctorParameterType, out var ctorParameterTypeFactory)
                        ? ctorParameterTypeFactory()!
                        : Create(ctorParameterType, visitedTypes);
                }
                // ensure no null constructor ctorArguments: replace nulls with AutoFaker-generated defaults
                for (var i = 0; i < ctorParameterTypes.Length; i++)
                {
                    if (ctorArguments[i] == null)
                    {
                        ctorArguments[i] = AutoFakerGenerate(ctorParameterTypes[i].ParameterType);
                    }
                }
                return ctor.Invoke(ctorArguments)!;
            }
            catch
            {
                return AutoFakerGenerate(type);
            }
            finally
            {
                visitedTypes.Remove(type);
            }
        }

        private static object AutoFakerGenerate(Type type) {
            var method = AutoFakerGenerateCache.GetOrAdd(type, t => {
                var methodDef = AutoFakerGenerateNoParam ?? AutoFakerGenerateWithParam ?? throw new System.InvalidOperationException("AutoFaker.Generate<T> not found");
                var closed = methodDef.MakeGenericMethod(t);
                var callExpr = default(MethodCallExpression)!;
                var parameters = closed.GetParameters();
                if (parameters.Length == 0) {
                    callExpr = Expression.Call(closed);
                } else if (parameters.Length == 1) {
                    // pass null for optional configure parameter
                    var paramType = parameters[0].ParameterType;
                    var nullConst = Expression.Constant(null, paramType);
                    callExpr = Expression.Call(closed, nullConst);
                } else {
                    throw new System.InvalidOperationException("AutoFaker.Generate has unexpected parameter count");
                }
                var convert = Expression.Convert(callExpr, typeof(object));
                return Expression.Lambda<Func<object>>(convert).Compile();
            });
            return method();
        }

        private IList CreateListInstance(Type elemType, int repeat, HashSet<Type> visitedTypes) {
            var listType = typeof(List<>).MakeGenericType(elemType);
            var list = (IList)Activator.CreateInstance(listType)!;
            for (var i = 0; i < repeat; i++)
                list.Add(Create(elemType, visitedTypes));
            return list;
        }

        private object CreateHashSetInstance(Type elemType, int repeat, HashSet<Type> visitedTypes) {
            var setType = typeof(HashSet<>).MakeGenericType(elemType);
            var set = (IEnumerable)Activator.CreateInstance(setType)!;
            var add = setType.GetMethod("Add")!;
            for (var i = 0; i < repeat; i++)
                add.Invoke(set, new[] { Create(elemType, visitedTypes) });
            return set!;
        }

        private System.Collections.IDictionary CreateDictionaryInstance(Type keyType, Type valType, int repeat, HashSet<Type> visitedTypes) {
            var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
            var dict = (System.Collections.IDictionary)Activator.CreateInstance(dictType)!;
            for (var i = 0; i < repeat; i++) {
                object key = keyType == typeof(string) ? (object)$"k{i}" : Create(keyType, visitedTypes);
                var val = Create(valType, visitedTypes);
                dict.Add(key, val);
            }
            return dict;
        }

        private static Func<object, object> CompileCreateRangeDelegate(MethodInfo genericMethodDef, Type elemType) {
            var closed = genericMethodDef.MakeGenericMethod(elemType);
            var param = Expression.Parameter(typeof(object), "items");
            // convert parameter to IEnumerable<elemType>
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elemType);
            var converted = Expression.Convert(param, enumerableType);
            var call = Expression.Call(closed, converted);
            var convertResult = Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object, object>>(convertResult, param).Compile();
        }

        private static Func<object, object> CompileCreateRangeDelegate(MethodInfo genericMethodDef, Type keyType, Type valType) {
            var closed = genericMethodDef.MakeGenericMethod(keyType, valType);
            var param = Expression.Parameter(typeof(object), "items");
            var kvType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valType);
            var enumerableKv = typeof(IEnumerable<>).MakeGenericType(kvType);
            var converted = Expression.Convert(param, enumerableKv);
            var call = Expression.Call(closed, converted);
            var convertResult = Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object, object>>(convertResult, param).Compile();
        }
    }
}


