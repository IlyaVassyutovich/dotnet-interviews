namespace Altenar;

public class Dict
{
    [Fact]
    public void Class__GHC_Overridden__Equals_Base()
    {
        var result = Foo(x => new KeyV1(x));

        Assert.Null(result);
    }

    [Fact]
    public void RecordClass__GHC_Overriden__Equals_Base()
    {
        var result = Foo(x => new KeyV2(x));

        Assert.Equal(69M, result);
    }

    [Fact]
    public void Class__GHC_Const__Equals_Base()
    {
        var result = Foo(x => new KeyV3(x));

        Assert.Null(result);
    }

    [Fact]
    public void Struct__GHC_Overridden__Equals_Base()
    {
        var result = Foo(x => new KeyV4(x));

        Assert.Equal(69M, result);
    }

    private static decimal? Foo<TKey>(Func<long, TKey> keyFactory) where TKey : notnull
    {
        var dict = new Dictionary<TKey, decimal>();

        const long innerKey = 42L;
        var key1wrapped = keyFactory(innerKey);
        dict[key1wrapped] = 69M;

        var key2wrapped = keyFactory(innerKey);
        return dict.TryGetValue(key2wrapped, out var result) ? result : null;
    }

    private sealed class KeyV1
    {
        public KeyV1(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public override int GetHashCode() => HashCode.Combine(Value);
    }

    private sealed record KeyV2(long Value)
    {
        public override int GetHashCode() => HashCode.Combine(Value);
    }

    private sealed class KeyV3
    {
        public KeyV3(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public override int GetHashCode() => 13;
    }

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