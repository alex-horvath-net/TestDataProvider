using System.Collections.Immutable;
using Common;

namespace AutoBogusTest {
    public class ExampleTypesTests {
        private BogusFixture fixture;

        public ExampleTypesTests() {
            fixture = new BogusFixture();
            fixture.RepeatCount = 3;
            fixture.Register(() => fixture.CreateMany<int>().ToImmutableArray());
            fixture.Register(() => fixture.CreateMany<string>().ToImmutableList());
            fixture.Register(() => fixture.CreateMany<string>().ToImmutableDictionary(x => x, x => x.Length));
            fixture.Register(() => fixture.CreateMany<int>().ToImmutableHashSet());
        }
        [Fact]
        public void AutoBogus_Creates_ExampleClass() {
            
            var ex = fixture.Create<ExampleClass>();

            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
            Assert.True(ex.PrimitiveInt > 0);
            Assert.True(ex.Other != null);
            Assert.Equal(3, ex.Array.Length);
            Assert.Equal(3, ex.ImmutableArr.Length);
            Assert.Equal(3, ex.List.Count);
            Assert.Equal(3, ex.ImmutableLst.Count);
            Assert.Equal(3, ex.Dictionary.Count);
            Assert.Equal(3, ex.ImmutableDict.Count);
            Assert.Equal(3, ex.HashSet.Count);
            Assert.Equal(3, ex.ImmutableSet.Count);
        }

        [Fact]
        public void AutoBogus_Creates_ExampleRecord() {
            var ex = fixture.Create<ExampleRecord>();

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


