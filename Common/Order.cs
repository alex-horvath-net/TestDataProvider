using System.Collections.Immutable;

namespace Common {
    public class Order { 
        public Order(int id, string product) { 
            Id = id; 
            Product = product; 
        } 
        
        // additional constructor accepting an immutable array
        public Order(int id, string product, ImmutableArray<int> codes) : this(id, product) {
            Codes = codes;
        }

        public int Id { get; } 
        public string Product { get; } 
        public ImmutableArray<int> Codes { get; } = ImmutableArray<int>.Empty;
    }
}
