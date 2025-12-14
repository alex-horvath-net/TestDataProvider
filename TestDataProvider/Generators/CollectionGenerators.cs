using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TestDataProvider.Generators
{
    public class CollectionGenerators
    {
        public object? TryCreate(Type t, object provider, int repeatCount)
        {
            // arrays
            if (t.IsArray)
            {
                var gen = t.GetElementType()!;
                var arr = Array.CreateInstance(gen, repeatCount);
                for (int i = 0; i < repeatCount; i++)
                {
                    var val = InvokeCreate(provider, gen);
                    arr.SetValue(val, i);
                }
                return arr;
            }

            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                var gen = t.GetGenericArguments()[0];
                if (def == typeof(List<>))
                {
                    var listType = typeof(List<>).MakeGenericType(gen);
                    var list = (IList)Activator.CreateInstance(listType)!;
                    for (int i = 0; i < repeatCount; i++) list.Add(InvokeCreate(provider, gen));
                    return list;
                }

                if (def == typeof(IEnumerable<>))
                {
                    var listType = typeof(List<>).MakeGenericType(gen);
                    var list = (IList)Activator.CreateInstance(listType)!;
                    for (int i = 0; i < repeatCount; i++) list.Add(InvokeCreate(provider, gen));
                    return list;
                }

                if (def == typeof(IReadOnlyList<>))
                {
                    var listType = typeof(List<>).MakeGenericType(gen);
                    var list = (IList)Activator.CreateInstance(listType)!;
                    for (int i = 0; i < repeatCount; i++) list.Add(InvokeCreate(provider, gen));
                    return list;
                }

                if (def == typeof(IDictionary<,>))
                {
                    var key = t.GetGenericArguments()[0];
                    var val = t.GetGenericArguments()[1];
                    var dictType = typeof(Dictionary<,>).MakeGenericType(key, val);
                    var dict = (IDictionary)Activator.CreateInstance(dictType)!;
                    for (int i = 0; i < repeatCount; i++) dict.Add(InvokeCreate(provider, key), InvokeCreate(provider, val));
                    return dict;
                }
            }

            return null;
        }

        static object? InvokeCreate(object provider, Type t)
        {
            var mi = provider.GetType().GetMethod("Create", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(int) }, null);
            if (mi != null)
                return mi.Invoke(provider, new object[] { t, 1 });
            // fallback
            var fallback = provider.GetType().GetMethod("Create", new[] { typeof(Type) });
            if (fallback != null)
                return fallback.Invoke(provider, new object[] { t });
            return null;
        }
    }
}
