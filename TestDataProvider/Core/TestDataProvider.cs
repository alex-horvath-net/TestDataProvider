using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using TestDataProvider.Caching;
using TestDataProvider.Generators;
// using TestDataProvider.Random; // avoid name conflict with System.Random
using TestDataProvider.Resolvers;
using TestDataProvider.Immutable;

namespace TestDataProviderCore
{
    public class Provider
    {
        readonly ActivatorCache _cache = new();
        readonly PrimitiveGenerator _primitives = new();
        readonly CollectionGenerators _collections = new();
        readonly TestDataProvider.Immutable.ImmutableHandlers _immutable = new TestDataProvider.Immutable.ImmutableHandlers();
        readonly ConcurrentDictionary<Type, Delegate> _registered = new();
        readonly List<Func<Type, object?>> _customizers = new();

        // reflection caches to avoid repeated MakeGenericMethod/Invoke overhead
        readonly ConcurrentDictionary<string, Func<object?[], object?>> _staticInvokerCache = new();
        readonly ConcurrentDictionary<Type, Func<object?>> _emptyPropCache = new();

        public int RepeatCount { get; set; } = 3;
        public int MaxDepth { get; set; } = 6;

        Func<object?[], object?> GetStaticInvoker(Type helperType, string methodName, params Type[] genArgs)
        {
            var key = helperType.FullName + ":" + methodName + ":" + string.Join(",", genArgs.Select(t => t.FullName ?? t.Name));
            return _staticInvokerCache.GetOrAdd(key, _ =>
            {
                var mi = helperType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == methodName && m.IsGenericMethod);
                var constructed = mi.MakeGenericMethod(genArgs);
                return _cache.GetFactoryInvoker(constructed);
            });
        }

        Func<object?> GetEmptyGetter(Type genericType)
        {
            return _emptyPropCache.GetOrAdd(genericType, t =>
            {
                var emptyProp = t.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static);
                if (emptyProp == null) return (Func<object?>)(() => null);
                return () => emptyProp.GetValue(null);
            });
        }

        public Provider()
        {
            // default customizers (primitives/collections) - keep order
            _customizers.Add(t => _primitives.TryCreate(t));
            _customizers.Add(t => _immutable.TryCreate(t, this, RepeatCount));
            _customizers.Add(t => _collections.TryCreate(t, this, RepeatCount));

            // robust fallback for ImmutableHashSet/TDictionary in case handler misses it
            _customizers.Add(t =>
            {
                try
                {
                    if (!t.IsGenericType) return null;
                    var def = t.GetGenericTypeDefinition();
                    if (def.FullName != null && def.FullName.Contains("ImmutableHashSet`1"))
                    {
                        var elem = t.GetGenericArguments()[0];
                        var listType = typeof(List<>).MakeGenericType(elem);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        // ensure we create RepeatCount unique elements (avoid set deduplication reducing count)
                        var seen = new HashSet<object?>();
                        int attempts = 0;
                        while (list.Count < RepeatCount && attempts < RepeatCount * 10)
                        {
                            attempts++;
                            var v = CreateInternal(elem, 1) ?? _primitives.TryCreate(elem) ?? (elem.IsValueType ? Activator.CreateInstance(elem) : (object?)Guid.NewGuid().ToString());
                            if (seen.Add(v)) list.Add(v);
                        }
                        var invoker = GetStaticInvoker(typeof(ImmutableHashSet), "CreateRange", elem);
                        return invoker(new object[] { list });
                    }

                    if (def.FullName != null && def.FullName.Contains("ImmutableDictionary`2"))
                    {
                        var key = t.GetGenericArguments()[0];
                        var val = t.GetGenericArguments()[1];
                        var kv = typeof(KeyValuePair<,>).MakeGenericType(key, val);
                        var listType = typeof(List<>).MakeGenericType(kv);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        var seen = new HashSet<object?>();
                        int attempts = 0;
                        while (list.Count < RepeatCount && attempts < RepeatCount * 10)
                        {
                            attempts++;
                            var k = CreateInternal(key, 1) ?? _primitives.TryCreate(key) ?? (key.IsValueType ? Activator.CreateInstance(key) : (object?)Guid.NewGuid().ToString());
                            var v = CreateInternal(val, 1) ?? _primitives.TryCreate(val) ?? (val.IsValueType ? Activator.CreateInstance(val) : null);
                            var kvp = Activator.CreateInstance(kv, k, v)!;
                            if (seen.Add(kvp)) list.Add(kvp);
                        }
                        var invoker = GetStaticInvoker(typeof(ImmutableDictionary), "CreateRange", key, val);
                        return invoker(new object[] { list });
                    }
                }
                catch { }
                return null;
            });
        }

        public void Register<T>(Func<T> factory)
        {
            _registered[typeof(T)] = factory!;
        }

        public void Customize(Func<Type, object?> customizer)
        {
            _customizers.Insert(0, customizer);
        }

        public T Create<T>()
        {
            var t = typeof(T);
            if (t.IsGenericType)
            {
                var defFull = t.GetGenericTypeDefinition().FullName ?? string.Empty;
                var args = t.GetGenericArguments();

                try
                {
                    if (defFull.Contains("ImmutableList`1"))
                    {
                        var listType = typeof(List<>).MakeGenericType(args[0]);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        for (int i = 0; i < RepeatCount; i++) list.Add(CreateElement(args[0], 1));
                        var invoker = GetStaticInvoker(typeof(ImmutableList), "CreateRange", args[0]);
                        return (T)invoker(new object[] { list })!;
                    }

                    if (defFull.Contains("ImmutableArray`1"))
                    {
                        var listType = typeof(List<>).MakeGenericType(args[0]);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        for (int i = 0; i < RepeatCount; i++) list.Add(CreateElement(args[0], 1));
                        var invoker = GetStaticInvoker(typeof(ImmutableArray), "CreateRange", args[0]);
                        return (T)invoker(new object[] { list })!;
                    }

                    // handle mutable HashSet<T>
                    if (t.GetGenericTypeDefinition() == typeof(HashSet<>))
                    {
                        var elem = args[0];
                        var listType = typeof(List<>).MakeGenericType(elem);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        var seen = new HashSet<object?>();
                        int attempts = 0;
                        while (list.Count < RepeatCount && attempts < RepeatCount * 10)
                        {
                            attempts++;
                            var v = CreateElement(elem, 1) ?? _primitives.TryCreate(elem) ?? (elem.IsValueType ? Activator.CreateInstance(elem) : (object?)Guid.NewGuid().ToString());
                            if (seen.Add(v)) list.Add(v);
                        }
                        var setType = typeof(HashSet<>).MakeGenericType(elem);
                        try
                        {
                            var created = Activator.CreateInstance(setType, new object[] { list });
                            if (created != null) return (T)created!;
                        }
                        catch { }
                    }

                    if (defFull.Contains("ImmutableHashSet`1"))
                    {
                        var elem = args[0];
                        var listType = typeof(List<>).MakeGenericType(elem);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        var seen = new HashSet<object?>();
                        int attempts = 0;
                        while (list.Count < RepeatCount && attempts < RepeatCount * 10)
                        {
                            attempts++;
                            var v = CreateElement(args[0], 1) ?? _primitives.TryCreate(elem) ?? (elem.IsValueType ? Activator.CreateInstance(elem) : (object?)Guid.NewGuid().ToString());
                            if (seen.Add(v)) list.Add(v);
                        }
                        var invoker = GetStaticInvoker(typeof(ImmutableHashSet), "CreateRange", elem);
                        var created = invoker(new object[] { list });
                        if (created != null) return (T)created!;
                        // fallback via Empty.Add chain
                        var genSetType = typeof(ImmutableHashSet<>).MakeGenericType(elem);
                        var current = GetEmptyGetter(genSetType)();
                        var addMethod = genSetType.GetMethod("Add", new[] { elem });
                        if (current != null && addMethod != null)
                        {
                            for (int i = 0; i < RepeatCount; i++)
                            {
                                var v = CreateElement(elem, 1) ?? _primitives.TryCreate(elem) ?? (elem.IsValueType ? Activator.CreateInstance(elem) : (object?)Guid.NewGuid().ToString());
                                current = addMethod.Invoke(current, new object[] { v });
                            }
                            return (T)current!;
                        }
                    }

                    if (defFull.Contains("ImmutableDictionary`2"))
                    {
                        var key = args[0];
                        var val = args[1];
                        // use builder for robust creation
                        var createBuilderInvoker = GetStaticInvoker(typeof(ImmutableDictionary), "CreateBuilder", key, val);
                        var builder = createBuilderInvoker(Array.Empty<object>());
                        if (builder != null)
                        {
                            var addMethod = builder.GetType().GetMethod("Add", new[] { key, val });
                            for (int i = 0; i < RepeatCount; i++)
                            {
                                var k = CreateElement(key, 1) ?? _primitives.TryCreate(key) ?? (key.IsValueType ? Activator.CreateInstance(key) : (object?)Guid.NewGuid().ToString());
                                var v = CreateElement(val, 1) ?? _primitives.TryCreate(val) ?? (val.IsValueType ? Activator.CreateInstance(val) : null);
                                addMethod?.Invoke(builder, new object[] { k, v });
                            }
                            var toImmutable = builder.GetType().GetMethod("ToImmutable");
                            var created = toImmutable?.Invoke(builder, null);
                            if (created != null) return (T)created!;
                        }
                    }
                }
                catch { }
            }

            return (T)Create(typeof(T), 0)!;
        }

        // public wrapper used by collection/immutable helpers via reflection
        // Call CreateInternal with depth=1 so helper calls do not reset recursion depth
        public object? Create(Type t) => CreateInternal(t, 1);

        public IEnumerable<T> CreateMany<T>(int count = -1)
        {
            if (count <= 0) count = RepeatCount;
            for (int i = 0; i < count; i++) yield return Create<T>();
        }

        object? Create(Type t, int depth)
        {
            if (depth > MaxDepth) return null;

            // direct handling for immutable collection types
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                var args = t.GetGenericArguments();
                var defName = def.FullName ?? string.Empty;
                try
                {
                    if (def.FullName != null && def.FullName.Contains("ImmutableList`1"))
                    {
                        var listType = typeof(List<>).MakeGenericType(args[0]);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        for (int i = 0; i < RepeatCount; i++) list.Add(CreateElement(args[0], depth + 1));
                        var invoker = GetStaticInvoker(typeof(ImmutableList), "CreateRange", args[0]);
                        return invoker(new object[] { list });
                    }

                    if (def.FullName != null && def.FullName.Contains("ImmutableArray`1"))
                    {
                        var listType = typeof(List<>).MakeGenericType(args[0]);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        for (int i = 0; i < RepeatCount; i++) list.Add(CreateElement(args[0], depth + 1));
                        var invoker = GetStaticInvoker(typeof(ImmutableArray), "CreateRange", args[0]);
                        return invoker(new object[] { list });
                    }

                    // handle mutable HashSet<T>
                    if (t.GetGenericTypeDefinition() == typeof(HashSet<>))
                    {
                        var elem = args[0];
                        var listType = typeof(List<>).MakeGenericType(elem);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        var seen = new HashSet<object?>();
                        int attempts = 0;
                        while (list.Count < RepeatCount && attempts < RepeatCount * 10)
                        {
                            attempts++;
                            var v = CreateElement(elem, depth + 1) ?? _primitives.TryCreate(elem) ?? (elem.IsValueType ? Activator.CreateInstance(elem) : (object?)Guid.NewGuid().ToString());
                            if (seen.Add(v)) list.Add(v);
                        }
                        var setType = typeof(HashSet<>).MakeGenericType(elem);
                        try
                        {
                            var created = Activator.CreateInstance(setType, new object[] { list });
                            if (created != null) return created;
                        }
                        catch { }
                    }

                    if (defName.Contains("ImmutableHashSet`1"))
                    {
                        var elem = args[0];
                        var listType = typeof(List<>).MakeGenericType(elem);
                        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                        var seen = new HashSet<object?>();
                        int attempts = 0;
                        while (list.Count < RepeatCount && attempts < RepeatCount * 10)
                        {
                            attempts++;
                            var v = CreateElement(elem, depth + 1) ?? _primitives.TryCreate(elem) ?? (elem.IsValueType ? Activator.CreateInstance(elem) : (object?)Guid.NewGuid().ToString());
                            if (seen.Add(v)) list.Add(v);
                        }
                        var invoker = GetStaticInvoker(typeof(ImmutableHashSet), "CreateRange", elem);
                        var created = invoker(new object[] { list });
                        if (created != null) return created;
                        // fallback: build via ImmutableHashSet<T>.Empty.Add(...)
                        var genSetType = typeof(ImmutableHashSet<>).MakeGenericType(elem);
                        var current = GetEmptyGetter(genSetType)();
                        var addMethod = genSetType.GetMethod("Add", new[] { elem });
                        if (current != null && addMethod != null)
                        {
                            for (int i = 0; i < RepeatCount; i++)
                            {
                                var v = CreateElement(elem, depth + 1) ?? _primitives.TryCreate(elem) ?? (elem.IsValueType ? Activator.CreateInstance(elem) : (object?)Guid.NewGuid().ToString());
                                current = addMethod.Invoke(current, new object[] { v });
                            }
                            return current;
                        }
                    }

                    if (defName.Contains("ImmutableDictionary`2"))
                    {
                        var key = args[0];
                        var val = args[1];
                        // use builder for robust creation
                        var createBuilderInvoker = GetStaticInvoker(typeof(ImmutableDictionary), "CreateBuilder", key, val);
                        var builder = createBuilderInvoker(Array.Empty<object>());
                        if (builder != null)
                        {
                            var addMethod = builder.GetType().GetMethod("Add", new[] { key, val });
                            for (int i = 0; i < RepeatCount; i++)
                            {
                                var k = CreateElement(key, depth + 1) ?? _primitives.TryCreate(key) ?? (key.IsValueType ? Activator.CreateInstance(key) : (object?)Guid.NewGuid().ToString());
                                var v = CreateElement(val, depth + 1) ?? _primitives.TryCreate(val) ?? (val.IsValueType ? Activator.CreateInstance(val) : null);
                                addMethod?.Invoke(builder, new object[] { k, v });
                            }
                            var toImmutable = builder.GetType().GetMethod("ToImmutable");
                            var created = toImmutable?.Invoke(builder, null);
                            if (created != null) return created;
                        }
                    }
                }
                catch { }
            }

            if (_registered.TryGetValue(t, out var del))
            {
                return del.DynamicInvoke();
            }

            // customizers
            foreach (var c in _customizers)
            {
                try
                {
                    var r = c(t);
                    if (r != null) return r;
                }
                catch { }
            }

            // enums
            if (t.IsEnum)
            {
                var vals = Enum.GetValues(t);
                return vals.Length == 0 ? Activator.CreateInstance(t) : vals.GetValue(new System.Random().Next(vals.Length));
            }

            // try factory static methods
            // prefer explicit sample factory CreateSample() if present
            var sampleFactory = t.GetMethod("CreateSample", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (sampleFactory != null && t.IsAssignableFrom(sampleFactory.ReturnType))
            {
                var invokerSample = _cache.GetFactoryInvoker(sampleFactory);
                try { return invokerSample(Array.Empty<object>()); } catch { }
            }

             var factory = ConstructorResolver.FindFactory(t);
             if (factory != null)
             {
                 var invoker = _cache.GetFactoryInvoker(factory);
                 var args = factory.GetParameters().Select(p => Create(p.ParameterType, depth + 1)).ToArray();
                 return invoker(args);
             }

             // constructors
             var ctor = ConstructorResolver.FindGreediestConstructor(t);
             if (ctor != null)
             {
                 var invoker = _cache.GetCtorInvoker(ctor);
                 var args = ctor.GetParameters().Select(p => Create(p.ParameterType, depth + 1)).ToArray();
                 return invoker(args);
             }

             // parameterless fallback
             try { return Activator.CreateInstance(t); } catch { }

             // final fallback: return empty immutable collections for known types to avoid nulls
             if (t.IsGenericType)
             {
                 var defName = t.GetGenericTypeDefinition().FullName ?? string.Empty;
                 try
                 {
                     if (defName.Contains("ImmutableList`1"))
                     {
                         var mi = typeof(ImmutableList).GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .First(m => m.Name == "CreateRange" && m.IsGenericMethod).MakeGenericMethod(t.GetGenericArguments()[0]);
                         var empty = Activator.CreateInstance(typeof(List<>).MakeGenericType(t.GetGenericArguments()[0]));
                         return mi.Invoke(null, new object[] { empty });
                     }
                     if (defName.Contains("ImmutableArray`1"))
                     {
                         var mi = typeof(ImmutableArray).GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .First(m => m.Name == "CreateRange" && m.IsGenericMethod).MakeGenericMethod(t.GetGenericArguments()[0]);
                         var empty = Activator.CreateInstance(typeof(List<>).MakeGenericType(t.GetGenericArguments()[0]));
                         return mi.Invoke(null, new object[] { empty });
                     }
                     if (defName.Contains("ImmutableHashSet`1"))
                     {
                         var gen = typeof(ImmutableHashSet<>).MakeGenericType(t.GetGenericArguments()[0]);
                         var emptyProp = gen.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static);
                         if (emptyProp != null) return emptyProp.GetValue(null);
                     }
                     if (defName.Contains("ImmutableDictionary`2"))
                     {
                         var gen = typeof(ImmutableDictionary<,>).MakeGenericType(t.GetGenericArguments());
                         var emptyProp = gen.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static);
                         if (emptyProp != null) return emptyProp.GetValue(null);
                     }
                 }
                 catch { }
             }

             return null;
        }

        // internal helpers used by generators
        internal object? CreateInternal(Type t, int depth) => Create(t, depth);

        object? CreateElement(Type t, int depth)
        {
            var res = CreateInternal(t, depth);
            if (res != null) return res;
            // try primitive generator
            res = _primitives.TryCreate(t);
            if (res != null) return res;
            // try parameterless
            try { return Activator.CreateInstance(t); } catch { }
            return null;
        }
    }
}
