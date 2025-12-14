using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace TestDataProvider.Caching
{
    public class ActivatorCache
    {
        readonly ConcurrentDictionary<ConstructorInfo, Func<object?[], object?>> _ctors = new();
        readonly ConcurrentDictionary<MethodInfo, Func<object?[], object?>> _factories = new();

        public Func<object?[], object?> GetCtorInvoker(ConstructorInfo ctor)
        {
            return _ctors.GetOrAdd(ctor, ci => CreateCtorDelegate(ci));
        }

        public Func<object?[], object?> GetFactoryInvoker(MethodInfo mi)
        {
            return _factories.GetOrAdd(mi, m => CreateFactoryDelegate(m));
        }

        Func<object?[], object?> CreateCtorDelegate(ConstructorInfo ci)
        {
            var parms = ci.GetParameters();
            var param = Expression.Parameter(typeof(object[]), "args");
            var args = new Expression[parms.Length];
            for (int i = 0; i < parms.Length; i++)
            {
                var idx = Expression.Constant(i);
                var access = Expression.ArrayIndex(param, idx);
                args[i] = Expression.Convert(access, parms[i].ParameterType);
            }
            var newExp = Expression.New(ci, args);
            var lambda = Expression.Lambda<Func<object?[], object?>>(Expression.Convert(newExp, typeof(object)), param);
            return lambda.Compile();
        }

        Func<object?[], object?> CreateFactoryDelegate(MethodInfo mi)
        {
            var parms = mi.GetParameters();
            var param = Expression.Parameter(typeof(object[]), "args");
            var args = new Expression[parms.Length];
            for (int i = 0; i < parms.Length; i++)
            {
                var idx = Expression.Constant(i);
                var access = Expression.ArrayIndex(param, idx);
                args[i] = Expression.Convert(access, parms[i].ParameterType);
            }
            var call = Expression.Call(mi, args);
            var lambda = Expression.Lambda<Func<object?[], object?>>(Expression.Convert(call, typeof(object)), param);
            return lambda.Compile();
        }
    }
}
