using System.Linq.Expressions;
using System.Reflection;

namespace Common;

public sealed class BogusFixtureBuilder<T> {
    private readonly BogusFixture _fixture;
    private readonly List<Action<T>> _mutators = new();

    internal BogusFixtureBuilder(BogusFixture fixture) {
        _fixture = fixture;
    }

    public BogusFixtureBuilder<T> With<TValue>(Expression<Func<T, TValue>> selector, TValue value) {
        var setter = CompileSetter(selector);
        _mutators.Add(target => setter(target, value));
        return this;
    }

    public BogusFixtureBuilder<T> Without<TValue>(Expression<Func<T, TValue>> selector) {
        var setter = CompileSetter(selector);
        _mutators.Add(target => setter(target, default!));
        return this;
    }

    public T Create() {
        var instance = _fixture.Create<T>();
        for (var i = 0; i < _mutators.Count; i++)
            _mutators[i](instance);
        return instance;
    }

    private static Action<T, TValue> CompileSetter<TValue>(Expression<Func<T, TValue>> selector) {
        ArgumentNullException.ThrowIfNull(selector);

        var memberExpr = selector.Body switch {
            MemberExpression m => m,
            UnaryExpression { Operand: MemberExpression m } => m,
            _ => throw new ArgumentException("Selector must target a writable property or field", nameof(selector))
        };

        if (memberExpr.Member is PropertyInfo prop) {
            if (!prop.CanWrite)
                throw new ArgumentException("Property must be writable", nameof(selector));

            var target = Expression.Parameter(typeof(T), "target");
            var val = Expression.Parameter(typeof(TValue), "value");
            var assign = Expression.Assign(Expression.Property(target, prop), val);
            return Expression.Lambda<Action<T, TValue>>(assign, target, val).Compile();
        }

        if (memberExpr.Member is FieldInfo field) {
            var target = Expression.Parameter(typeof(T), "target");
            var val = Expression.Parameter(typeof(TValue), "value");
            var assign = Expression.Assign(Expression.Field(target, field), val);
            return Expression.Lambda<Action<T, TValue>>(assign, target, val).Compile();
        }

        throw new ArgumentException("Selector must target a writable property or field", nameof(selector));
    }
}
