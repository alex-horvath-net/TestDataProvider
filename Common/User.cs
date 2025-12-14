using System.Collections.Immutable;

namespace Common {
    public record User(
        string Name, 
        int Age)
    {
        // additional constructor that accepts an immutable collection
        public ImmutableList<string>? Tags { get; }

        public User(ImmutableList<string>? tags)
            : this(tags != null && tags.Count > 0 ? tags[0] : string.Empty, tags?.Count ?? 0)
        {
            Tags = tags;
        }
    }
}
