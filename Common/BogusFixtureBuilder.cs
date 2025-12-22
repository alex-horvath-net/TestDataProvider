using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Common;

public sealed class BogusFixtureBuilder<T> {
    private readonly BogusFixture fixture;
    private readonly List<Func<T, T>> mutators = new();

    internal BogusFixtureBuilder(BogusFixture fixture) {
        this.fixture = fixture;
    }

    public BogusFixtureBuilder<T> With<TValue>(Expression<Func<T, TValue>> selector, TValue value) {
        var member = GetMember(selector);
        mutators.Add(target => ReconstructWith<TValue>(target, member, value));
        return this;
    }

    public BogusFixtureBuilder<T> Without<TValue>(Expression<Func<T, TValue>> selector) {
        var member = GetMember(selector);
        mutators.Add(target => ReconstructWith<TValue>(target, member, default!));
        return this;
    }

    public T Create() {
        var instance = fixture.Create<T>();
        for (var i = 0; i < mutators.Count; i++) {
            instance = mutators[i](instance);
        }

        return instance;
    }

    public IEnumerable<T> CreateMany(int? count = null) {
        var total = count ?? fixture.RepeatCount;
        for (var i = 0; i < total; i++) {
            var instance = fixture.Create<T>();
            for (var j = 0; j < mutators.Count; j++) {
                instance = mutators[j](instance);
            }

            yield return instance;
        }
    }

    private static MemberInfo GetMember<TValue>(Expression<Func<T, TValue>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return selector.Body switch
        {
            MemberExpression m => m.Member,
            UnaryExpression { Operand: MemberExpression m } => m.Member,
            _ => throw new ArgumentException("Selector must target a property or field", nameof(selector))
        };
    }

    private static T ReconstructWith<TValue>(T source, MemberInfo member, TValue value)
    {
        var ctor = typeof(T).GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault() ?? throw new InvalidOperationException("Type must have a public constructor");

        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var current = GetMemberValue(source, param.Name);
            if (IsSameMember(member, param.Name)) {
                current = value;
            }

            args[i] = current;
        }

        return (T)ctor.Invoke(args);
    }

    private static object? GetMemberValue(object instance, string? name)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (name is null) {
            return null;
        }

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        var type = instance.GetType();

        var prop = type.GetProperty(name, Flags);
        if (prop != null) {
            return prop.GetValue(instance);
        }

        var field = type.GetField(name, Flags);
        return field?.GetValue(instance);
    }

    private static bool IsSameMember(MemberInfo member, string? name)
    {
        if (name is null) {
            return false;
        }

        return string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase);
    }
}
