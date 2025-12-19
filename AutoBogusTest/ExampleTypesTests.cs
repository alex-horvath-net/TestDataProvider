using System.Collections.Immutable;
using Common;

namespace AutoBogusTest {
    public class ExampleTypesTests {
        private BogusFixture fixture = FixtureFactory.CreateByAutoBogus();

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
    }
}


