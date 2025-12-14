//using Common;
//using TestDataProviderCore;

//namespace TestDataProviderTest
//{
//    public class ExampleTypesTests
//    {

//        [Fact]
//        public void Provider_Creates_ExampleClass()
//        {
//            var provider = new Provider();
//            var ex = provider.Create<ExampleClass>();
//            Assert.NotNull(ex);
//            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
//            Assert.True(ex.PrimitiveInt > 0);
//            Assert.NotNull(ex.Array);
//            Assert.Equal(3, ex.Array.Length);
//            Assert.NotNull(ex.ImmutableArr);
//            Assert.Equal(3, ex.ImmutableArr.Length);
//            Assert.NotNull(ex.List);
//            Assert.Equal(3, ex.List.Count);
//            Assert.NotNull(ex.ImmutableLst);
//            Assert.Equal(3, ex.ImmutableLst.Count);
//            Assert.NotNull(ex.Dictionary);
//            Assert.Equal(3, ex.Dictionary.Count);
//            Assert.NotNull(ex.ImmutableDict);
//            Assert.Equal(3, ex.ImmutableDict.Count);
//            Assert.NotNull(ex.HashSet);
//            Assert.Equal(3, ex.HashSet.Count);
//            Assert.NotNull(ex.ImmutableSet);
//            Assert.Equal(3, ex.ImmutableSet.Count);
//        }

//        [Fact]
//        public void Provider_Creates_ExampleRecord()
//        {
//            var provider = new Provider();
//            var ex = provider.Create<ExampleRecord>();
//            Assert.NotNull(ex);
//            Assert.False(string.IsNullOrWhiteSpace(ex.PrimitiveString));
//            Assert.True(ex.PrimitiveInt > 0);
//            Assert.NotNull(ex.Array);
//            Assert.Equal(3, ex.Array.Length);
//            Assert.NotNull(ex.ImmutableArr);
//            Assert.Equal(3, ex.ImmutableArr.Length);
//            Assert.NotNull(ex.List);
//            Assert.Equal(3, ex.List.Count);
//            Assert.NotNull(ex.ImmutableLst);
//            Assert.Equal(3, ex.ImmutableLst.Count);
//            Assert.NotNull(ex.Dictionary);
//            Assert.Equal(3, ex.Dictionary.Count);
//            Assert.NotNull(ex.ImmutableDict);
//            Assert.Equal(3, ex.ImmutableDict.Count);
//            Assert.NotNull(ex.HashSet);
//            Assert.Equal(3, ex.HashSet.Count);
//            Assert.NotNull(ex.ImmutableSet);
//            Assert.Equal(3, ex.ImmutableSet.Count);
//        }
//    }
//}
