namespace xTests;

public class StorageTypeTests(ITestOutputHelper output)
{
    private struct Type1 { }

    [Fact]
    public void StorageType_is_Comparable()
    {
        var t1 = StorageType.Create<Type1>();
        var t2 = StorageType.Create<Type1>();
        Assert.Equal(t1, t2);
    }
    
    [Fact]
    public void StorageType_is_Distinct()
    {
        var t1 = StorageType.Create<int>();
        var t2 = StorageType.Create<ushort>();
        Assert.NotEqual(t1 , t2);
    }
    
    [Fact]
    public void StorageType_implicitly_decays_to_Type()
    {
        var t1 = StorageType.Create<Type1>();
        var t2 = typeof(Type1);
        Assert.Equal(t2, t1);
        Assert.Equal(t1, t2);
    }
}