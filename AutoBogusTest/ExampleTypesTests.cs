using Common;
using Xunit;

namespace AutoBogusTest {
    public class ExampleTypesTests {
        private static readonly BogusFixture fixture = FixtureFactory.CreateByBogus();

        [Fact]
        public void AutoBogus_Creates_ExampleClass() {
            var ex = fixture.Create<ExampleClass>();

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
        }

        [Fact]
        public void AutoBogus_Build_ExampleClass() {
            var link = new ExampleOtherRecord(new Uri("http://example.com"), "Example");
            var ex = fixture
                .Build<ExampleRecord>()
                .With(x => x.OtherRecord, link)
                .Create();

            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
            Assert.Equal(3, ex.Array.Length);
            Assert.Equal(3, ex.ImmutableArr.Length);
            Assert.Equal(3, ex.List.Count);
            Assert.Equal(3, ex.ImmutableLst.Count);
            Assert.Equal(3, ex.Dictionary.Count);
            Assert.Equal(3, ex.ImmutableDict.Count);
            Assert.Equal(3, ex.HashSet.Count);
            Assert.Equal(3, ex.ImmutableSet.Count);
            Assert.True(ex.OtherRecord != null);
            Assert.Equal("http://example.com/", ex.OtherRecord.Link.AbsoluteUri);
        }

        [Fact]
        public void AutoBogus_Creates_ExampleRecord() {
            var ex = fixture.Create<ExampleRecord>();

            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
            Assert.True(ex.PrimitiveInt > 0);
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



