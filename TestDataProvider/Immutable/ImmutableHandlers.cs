using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace TestDataProvider.Immutable
{
    public class ImmutableHandlers
    {
        public object? TryCreate(Type t, object provider, int repeatCount)
        {
            Console.WriteLine($"ImmutableHandlers.TryCreate called for {t.FullName}");

            if (!t.IsGenericType) return null;
            var def = t.GetGenericTypeDefinition();
            var args = t.GetGenericArguments();

            bool IsDef(Type type, string name) => def == type || (def.FullName != null && def.FullName.Contains(name));

            if (IsDef(typeof(ImmutableList<>), "ImmutableList`1"))
            {
                Console.WriteLine("Creating ImmutableList of " + args[0].FullName);
                var listType = typeof(List<>).MakeGenericType(args[0]);
                var list = (IList)Activator.CreateInstance(listType)!;
                for (int i = 0; i < repeatCount; i++)
                {
                    var v = InvokeCreate(provider, args[0]);
                    Console.WriteLine($"  element[{i}] -> {(v == null ? "null" : v.GetType().FullName)}");
                    list.Add(v);
                }
                var mi = typeof(ImmutableList).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "CreateRange" && m.IsGenericMethod).MakeGenericMethod(args[0]);
                var res = mi.Invoke(null, new object[] { list });
                Console.WriteLine($"ImmutableList created: {res?.GetType().FullName}");
                return res;
            }

            if (IsDef(typeof(ImmutableArray<>), "ImmutableArray`1"))
            {
                Console.WriteLine("Creating ImmutableArray of " + args[0].FullName);
                var listType = typeof(List<>).MakeGenericType(args[0]);
                var list = (IList)Activator.CreateInstance(listType)!;
                for (int i = 0; i < repeatCount; i++)
                {
                    var v = InvokeCreate(provider, args[0]);
                    Console.WriteLine($"  element[{i}] -> {(v == null ? "null" : v.GetType().FullName)}");
                    list.Add(v);
                }
                var mi = typeof(ImmutableArray).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "CreateRange" && m.IsGenericMethod).MakeGenericMethod(args[0]);
                var res = mi.Invoke(null, new object[] { list });
                Console.WriteLine($"ImmutableArray created: {res?.GetType().FullName}");
                return res;
            }

            if (IsDef(typeof(ImmutableHashSet<>), "ImmutableHashSet`1"))
            {
                Console.WriteLine("Creating ImmutableHashSet of " + args[0].FullName);
                var listType = typeof(List<>).MakeGenericType(args[0]);
                var list = (IList)Activator.CreateInstance(listType)!;

                for (int i = 0; i < repeatCount; i++)
                {
                    var val = InvokeCreate(provider, args[0]);
                    Console.WriteLine($"  element[{i}] -> {(val == null ? "null" : val.GetType().FullName)}");
                    if (val == null)
                    {
                        // fallback primitive-safe generators
                        if (args[0] == typeof(int)) val = i + 1;
                        else if (args[0] == typeof(string)) val = $"s_{i}_{Guid.NewGuid():N}";
                        else if (args[0] == typeof(Guid)) val = Guid.NewGuid();
                        else if (args[0] == typeof(DateTime)) val = DateTime.UtcNow.AddSeconds(i);
                        else if (args[0].IsValueType)
                        {
                            try { val = Activator.CreateInstance(args[0]); } catch { val = null; }
                        }
                        else
                        {
                            // try to create via parameterless constructor
                            try { val = Activator.CreateInstance(args[0]); } catch { val = null; }
                        }
                        Console.WriteLine($"    fallback -> {(val == null ? "null" : val.GetType().FullName)}");
                    }

                    list.Add(val);
                }

                // ensure uniqueness for set
                try
                {
                    var distinctList = typeof(Enumerable).GetMethod("Distinct", BindingFlags.Static | BindingFlags.Public)?.MakeGenericMethod(args[0]);
                    if (distinctList != null)
                    {
                        var distinct = distinctList.Invoke(null, new object[] { list });
                        var toList = typeof(Enumerable).GetMethod("ToList", BindingFlags.Static | BindingFlags.Public)?.MakeGenericMethod(args[0]);
                        if (toList != null)
                        {
                            var final = toList.Invoke(null, new object[] { distinct });
                            var mi = typeof(ImmutableHashSet).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                .First(m => m.Name == "CreateRange" && m.IsGenericMethod).MakeGenericMethod(args[0]);
                            var res = mi.Invoke(null, new object[] { final });
                            Console.WriteLine($"ImmutableHashSet created: {res?.GetType().FullName}");
                            if (res != null) return res;
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine($"ImmutableHashSet distinct error: {ex}"); }

                // last resort: try creating from the original list
                try
                {
                    var mi = typeof(ImmutableHashSet).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == "CreateRange" && m.IsGenericMethod).MakeGenericMethod(args[0]);
                    var res = mi.Invoke(null, new object[] { list });
                    Console.WriteLine($"ImmutableHashSet created (last resort): {res?.GetType().FullName}");
                    return res;
                }
                catch (Exception ex) { Console.WriteLine($"ImmutableHashSet final create error: {ex}"); }

                return null;
            }

            if (IsDef(typeof(ImmutableDictionary<,>), "ImmutableDictionary`2"))
            {
                Console.WriteLine("Creating ImmutableDictionary of " + args[0].FullName + "," + args[1].FullName);
                var kv = typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]);
                var listType = typeof(List<>).MakeGenericType(kv);
                var list = (IList)Activator.CreateInstance(listType)!;
                for (int i = 0; i < repeatCount; i++)
                {
                    var key = InvokeCreate(provider, args[0]);
                    var val = InvokeCreate(provider, args[1]);
                    Console.WriteLine($"  kv[{i}] -> key:{(key==null?"null":key.GetType().FullName)} val:{(val==null?"null":val.GetType().FullName)}");
                    var kvv = Activator.CreateInstance(kv, key, val)!;
                    list.Add(kvv);
                }
                var mi = typeof(ImmutableDictionary).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "CreateRange" && m.IsGenericMethod).MakeGenericMethod(args[0], args[1]);
                var res = mi.Invoke(null, new object[] { list });
                Console.WriteLine($"ImmutableDictionary created: {res?.GetType().FullName}");
                return res;
            }

            return null;
        }

        static object? InvokeCreate(object provider, Type t)
        {
            // prefer calling public generic Create<T>() => provider.Create<T>()
            try
            {
                var createGeneric = provider.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == "Create" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
                if (createGeneric != null)
                {
                    var gen = createGeneric.MakeGenericMethod(t);
                    var res = gen.Invoke(provider, Array.Empty<object>());
                    Console.WriteLine($"InvokeCreate generic {t.FullName} -> {(res==null?"null":res.GetType().FullName)}");
                    return res;
                }
            }
            catch (Exception ex) { Console.WriteLine($"InvokeCreate generic error: {ex}"); }

            // try internal CreateInternal(Type,int)
            var miInternal = provider.GetType().GetMethod("CreateInternal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(int) }, null);
            if (miInternal != null)
            {
                var res = miInternal.Invoke(provider, new object[] { t, 1 });
                Console.WriteLine($"InvokeCreate internal {t.FullName} -> {(res==null?"null":res.GetType().FullName)}");
                return res;
            }

            // try to find non-public Create(Type,int)
            var mi = provider.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(int) }, null);
            if (mi != null)
            {
                var res = mi.Invoke(provider, new object[] { t, 1 });
                Console.WriteLine($"InvokeCreate overload {t.FullName} -> {(res==null?"null":res.GetType().FullName)}");
                return res;
            }

            // fallback to public Create(Type)
            var fallback = provider.GetType().GetMethod("Create", new[] { typeof(Type) });
            if (fallback != null)
            {
                var res = fallback.Invoke(provider, new object[] { t });
                Console.WriteLine($"InvokeCreate fallback {t.FullName} -> {(res==null?"null":res.GetType().FullName)}");
                return res;
            }
            Console.WriteLine($"InvokeCreate {t.FullName} -> null (no method)");
            return null;
        }
    }
}
