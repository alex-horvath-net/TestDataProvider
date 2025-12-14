using AutoFixture;
using System.Collections.Immutable;
using System.Linq;
using Common;
using Xunit;

namespace AutoFixtureTest {
    public class ExampleTypesTests {
      
        [Fact]
        public void AutoFixture_Creates_ExampleClass() {
            var fixture = new Fixture();
            // avoid deep recursion issues
            fixture.Behaviors.Remove(fixture.Behaviors.OfType<ThrowingRecursionBehavior>().First());
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            fixture.RepeatCount =3;
            // register collections and complex types (everything except ExampleClass)
            fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
            fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
            fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
            fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());
            
            var ex = fixture.Create<ExampleClass>();
            //var ex = fixture.Build<ExampleClass>().With(x => x.PrimitiveInt, 42).Create();

            Assert.NotNull(ex);
            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
            Assert.True(ex.PrimitiveInt > 0);
            Assert.NotNull(ex.Array);
            Assert.Equal(3, ex.Array.Length);
            Assert.NotNull(ex.ImmutableArr);
            Assert.Equal(3, ex.ImmutableArr.Length);
            Assert.NotNull(ex.List);
            Assert.Equal(3, ex.List.Count);
            Assert.NotNull(ex.ImmutableLst);
            Assert.Equal(3, ex.ImmutableLst.Count);
            Assert.NotNull(ex.Dictionary);
            Assert.Equal(3, ex.Dictionary.Count);
            Assert.NotNull(ex.ImmutableDict);
            Assert.Equal(3, ex.ImmutableDict.Count);
            Assert.NotNull(ex.HashSet);
            Assert.Equal(3, ex.HashSet.Count);
            Assert.NotNull(ex.ImmutableSet);
            Assert.Equal(3, ex.ImmutableSet.Count);
        }

        [Fact]
        public void AutoFixture_Creates_ExampleRecord() {
            var fixture = new Fixture();
            fixture.Behaviors.Remove(fixture.Behaviors.OfType<ThrowingRecursionBehavior>().First());
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            // demonstrate Register: provide a custom factory for ExampleOtherClass
            //fixture.Register<ExampleOtherClass>(() => ExampleOtherClass.CreateSample());

            // register collections and complex types (everything except ExampleClass)
            fixture.Register<int[]>(() => fixture.CreateMany<int>(3).ToArray());
            fixture.Register<System.Collections.Generic.List<string>>(() => fixture.CreateMany<string>(3).ToList());
            fixture.Register<System.Collections.Generic.IEnumerable<string>>(() => fixture.CreateMany<string>(3).ToList());
            fixture.Register<ImmutableArray<int>>(() => ImmutableArray.CreateRange(fixture.CreateMany<int>(3)));
            fixture.Register<ImmutableList<string>>(() => ImmutableList.CreateRange(fixture.CreateMany<string>(3)));
            fixture.Register<System.Collections.Generic.Dictionary<string, int>>(() => {
                var d = new System.Collections.Generic.Dictionary<string, int>();
                for (int i = 0; i < 3; i++)
                    d[$"k{i}"] = fixture.Create<int>();
                return d;
            });
            fixture.Register<ImmutableDictionary<string, int>>(() => ImmutableDictionary.CreateRange(fixture.Create<System.Collections.Generic.Dictionary<string, int>>()));
            fixture.Register<System.Collections.Generic.HashSet<int>>(() => new System.Collections.Generic.HashSet<int>(fixture.CreateMany<int>(3)));
            fixture.Register<ImmutableHashSet<int>>(() => ImmutableHashSet.CreateRange(fixture.Create<System.Collections.Generic.HashSet<int>>()));
            //fixture.Register<ExampleRecord>(() => ExampleRecord.CreateSample());

            var primitiveInt = fixture.Create<int>();
            var primitiveString = fixture.Create<string>();
            var arr = fixture.CreateMany<int>(3).ToArray();
            var imArr = ImmutableArray.CreateRange(arr);
            var list = fixture.CreateMany<string>(3).ToList();
            var imList = ImmutableList.CreateRange(list);
            var dict = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < 3; i++)
                dict[$"k{i}"] = fixture.Create<int>();
            var imDict = ImmutableDictionary.CreateRange(dict);
            var hs = new System.Collections.Generic.HashSet<int>(fixture.CreateMany<int>(3));
            var imSet = ImmutableHashSet.CreateRange(hs);

            // use fixture.Create to get ExampleOtherClass via the registered factory
            var other = fixture.Create<ExampleOtherClass>();
            var ex = new ExampleRecord(primitiveInt, primitiveString, arr, imArr, list, list, imList, dict, imDict, hs, imSet, other);

            Assert.NotNull(ex);
            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
            Assert.True(ex.PrimitiveInt > 0);
            Assert.NotNull(ex.Array);
            Assert.Equal(3, ex.Array.Length);
            Assert.NotNull(ex.ImmutableArr);
            Assert.Equal(3, ex.ImmutableArr.Length);
            Assert.NotNull(ex.List);
            Assert.Equal(3, ex.List.Count);
            Assert.NotNull(ex.ImmutableLst);
            Assert.Equal(3, ex.ImmutableLst.Count);
            Assert.NotNull(ex.Dictionary);
            Assert.Equal(3, ex.Dictionary.Count);
            Assert.NotNull(ex.ImmutableDict);
            Assert.Equal(3, ex.ImmutableDict.Count);
            Assert.NotNull(ex.HashSet);
            Assert.Equal(3, ex.HashSet.Count);
            Assert.NotNull(ex.ImmutableSet);
            Assert.Equal(3, ex.ImmutableSet.Count);
        }
    }
}
