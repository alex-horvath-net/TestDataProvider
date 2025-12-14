using System;
using System.Linq;
using System.Reflection;

namespace TestDataProvider.Resolvers
{
    public static class ConstructorResolver
    {
        public static ConstructorInfo? FindGreediestConstructor(Type t)
        {
            if (t.IsAbstract || t.IsInterface) return null;
            var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (ctors.Length == 0) return null;
            return ctors.OrderByDescending(c => c.GetParameters().Length).First();
        }

        public static MethodInfo? FindFactory(Type t)
        {
            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
            var candidates = methods.Where(m => (m.Name == "Create" || m.Name == "Build" || m.Name == "From") && t.IsAssignableFrom(m.ReturnType)).ToArray();
            if (candidates.Length == 0) return null;
            // prefer greediest
            return candidates.OrderByDescending(m => m.GetParameters().Length).First();
        }
    }
}
