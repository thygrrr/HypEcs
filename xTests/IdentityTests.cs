namespace xTests;

using ECS;

public class IdentityTests(ITestOutputHelper output)
{
    [Fact]
    public void Identity_None_is_Zeros()
    {
        var none = Identity.None;
        Assert.Equal(default, none.Generation);
        output.WriteLine(none.Generation.ToString());
        output.WriteLine(none.ToString());
        Assert.Equal(default, none.Id);
    }

    [Fact]
    public void Identity_None_cannot_Match_One()
    {
        var zero = new Identity(0);
        Assert.NotEqual(Identity.None, zero);

        var one = new Identity(1);
        Assert.NotEqual(Identity.None, one);
    }

    [Fact]
    public void Identity_Matches_Only_Self()
    {
        var self = new Identity(12345);
        Assert.Equal(self, self);

        var successor = new Identity(12345, 3);
        Assert.NotEqual(self, successor);

        var other = new Identity(9000, 3);
        Assert.NotEqual(self, other);

    }

    [Fact]
    public void Identity_Matches_Self_if_Same()
    {
        var self = new Identity(12345, 6);
        var other = new Identity(12345, 6);
        Assert.Equal(self, other);
    }
}