using System;

namespace TestDataProvider.Random
{
    public class SimpleRandom
    {
        readonly System.Random _r = new System.Random();
        public int Next() => _r.Next();
        public int Next(int max) => _r.Next(max);
        public int Next(int min, int max) => _r.Next(min, max);
        public double NextDouble() => _r.NextDouble();
        public bool NextBool() => _r.Next(2) == 0;
    }
}
