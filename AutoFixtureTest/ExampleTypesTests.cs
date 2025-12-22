using System.Collections.Immutable;
using AutoFixture;
using Common;

namespace AutoFixtureTest {
    public class ExampleTypesTests {

        Fixture fixture = FixtureFactory.CreateByAutoFixture();

        [Fact]
        public void AutoFixture_Creates_ExampleClass() {
            var ex = fixture.Build<ExampleClass>().With(x => x.PrimitiveInt, 42).Create();
            //var ex = fixture.Build<ExampleClass>().With(x => x.PrimitiveInt, 42).Create();

            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
            Assert.True(ex.PrimitiveInt == 42);
            Assert.True(ex.Other != null);
            Assert.Equal(3, ex.Array.Length);
            Assert.Equal(3, ex.ImmutableArr.Length);
            Assert.Equal(3, ex.List.Count);
            Assert.Equal(3, ex.ImmutableLst.Count);
            Assert.Equal(3, ex.Dictionary.Count);
            Assert.Equal(3, ex.ImmutableDict.Count);
            Assert.Equal(3, ex.HashSet.Count);
            Assert.Equal(3, ex.ImmutableSet.Count);
            Assert.Equal(3, ex.ImmutableSet.Count);
        }

        [Fact]
        public void AutoFixture_Creates_ExampleRecord() {
            var ex = fixture.Build<ExampleRecord>().With(x => x.PrimitiveInt, 42).Create();

            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
            Assert.True(ex.PrimitiveInt == 42);
            Assert.True(ex.OtherRecord != null);
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
