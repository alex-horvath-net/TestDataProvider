//using System.Collections.Immutable;
//using Common;
//using TestDataProviderCore;

//namespace TestDataProviderTest {
//    public class TestDataProviderTests {
//        [Fact]
//        public void CanCreateClassRecordStructAndCollections() {
//            var provider = new Provider();
//            var user = provider.Create<User>();
//            Assert.False(string.IsNullOrWhiteSpace(user.Name));
//            Assert.True(user.Age > 0);

//            var orders = provider.CreateMany<Order>(5);
//            Assert.Equal(5, System.Linq.Enumerable.Count(orders));

//            var arr = provider.Create<int[]>();
//            Assert.Equal(3, arr.Length);

//            var list = provider.Create<System.Collections.Generic.List<string>>();
//            Assert.Equal(3, list.Count);

//            var imList = provider.Create<ImmutableList<string>>();
//            Assert.Equal(3, imList.Count);

//            var imArr = provider.Create<ImmutableArray<int>>();
//            Assert.Equal(3, imArr.Length);

//            var imSet = provider.Create<ImmutableHashSet<int>>();
//            Assert.Equal(3, imSet.Count);

//            var imDict = provider.Create<ImmutableDictionary<string, int>>();
//            Assert.Equal(3, imDict.Count);
//        }

//        [Fact]
//        public void RegisterOverridesCreation() {
//            var provider = new Provider();
//            provider.Register(() => new Foo("bar"));
//            var foo = provider.Create<Foo>();
//            Assert.Equal("bar", foo.Value);
//        }
//    }
//}
