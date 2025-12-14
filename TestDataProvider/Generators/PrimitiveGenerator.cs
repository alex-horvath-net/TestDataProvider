using System;
using TestDataProvider.Random;

namespace TestDataProvider.Generators
{
    public class PrimitiveGenerator
    {
        readonly SimpleRandom _r = new();

        public object? TryCreate(Type t)
        {
            if (t == typeof(string)) return RandomString();
            if (t == typeof(int)) return _r.Next(1, int.MaxValue);
            if (t == typeof(long)) return (long)_r.Next(1, int.MaxValue);
            if (t == typeof(bool)) return _r.NextBool();
            if (t == typeof(double)) return _r.NextDouble();
            if (t == typeof(float)) return (float)_r.NextDouble();
            if (t == typeof(DateTime)) return DateTime.UtcNow.AddSeconds(_r.Next());
            if (t == typeof(Guid)) return Guid.NewGuid();
            if (t.IsPrimitive) return Activator.CreateInstance(t);
            return null;
        }

        string RandomString()
        {
            var len = _r.Next(3, 10);
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            var arr = new char[len];
            for (int i = 0; i < len; i++) arr[i] = chars[_r.Next(chars.Length)];
            return new string(arr);
        }
    }
}
