namespace Altenar;

public class Dict
{
    [Fact]
    public void Class__GHC_Overridden__Equals_Base()
    {
        var result = LookupWithEqualKey(x => new KeyV1(x));

        Assert.Null(result);
    }

    [Fact]
    public void RecordClass__GHC_Overridden__Equals_Synthesized()
    {
        var result = LookupWithEqualKey(x => new KeyV2(x));

        Assert.Equal(69M, result);
    }

    [Fact]
    public void Class__GHC_Const__Equals_Base()
    {
        var result = LookupWithEqualKey(x => new KeyV3(x));

        Assert.Null(result);
    }

    [Fact]
    public void Struct__GHC_Overridden__Equals_Base()
    {
        var result = LookupWithEqualKey(x => new KeyV4(x));

        Assert.Equal(69M, result);
    }

    private static decimal? LookupWithEqualKey<TKey>(Func<long, TKey> keyFactory) where TKey : notnull
    {
        var dict = new Dictionary<TKey, decimal>();

        const long innerKey = 42L;
        var key1wrapped = keyFactory(innerKey);
        dict[key1wrapped] = 69M;

        // A second instance is built on purpose: the hash code only selects the bucket,
        // the entry is accepted only if the stored hash matches AND EqualityComparer<TKey>.Default.Equals
        // returns true. So the outcome depends on what Equals means for TKey, not on GetHashCode alone.
        var key2wrapped = keyFactory(innerKey);
        return dict.TryGetValue(key2wrapped, out var result) ? result : null;
    }

    // Miss: Equals is not overridden, so Object.Equals compares references. Both keys land in the same
    // bucket with the same hash, but they are different objects, so the equality check rejects the entry.
    private sealed class KeyV1
    {
        public KeyV1(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public override int GetHashCode() => HashCode.Combine(Value);
    }

    // Hit: the compiler synthesizes value-based Equals (and IEquatable<KeyV2>) for records, comparing
    // EqualityContract and every field. Overriding GetHashCode by hand does not suppress that synthesis,
    // and the custom hash stays consistent with it because it is derived from the same field.
    private sealed record KeyV2(long Value)
    {
        public override int GetHashCode() => HashCode.Combine(Value);
    }

    // Miss: equal hash codes are necessary but not sufficient. A constant hash only guarantees that every
    // key collides into one bucket (degrading lookups to O(n)); Equals is still reference-based, as in KeyV1.
    private sealed class KeyV3
    {
        public KeyV3(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public override int GetHashCode() => 13;
    }

    // Hit: structs inherit ValueType.Equals, which compares field values instead of references
    // (bitwise here, since the only field is a long; reflection-based when fields contain references or padding).
    // It works, but each comparison boxes the key because IEquatable<KeyV4> is not implemented.
    private struct KeyV4
    {
        public KeyV4(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public override int GetHashCode() => HashCode.Combine(Value);
    }
}
