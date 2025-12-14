using System.Collections.Immutable;

namespace Common {
    public class Foo { 
        public string Value { get; } 
        public Foo(string v) { Value = v; } 

        // additional constructor accepting an immutable hash set
        public Foo(ImmutableHashSet<int> nums) : this(nums != null && nums.Count > 0 ? nums.First().ToString() : string.Empty) {
            Numbers = nums;
        }

        public ImmutableHashSet<int>? Numbers { get; }
    }
}
