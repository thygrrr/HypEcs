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

    // [Fact(Skip = "Computationally Expensive")]
    // ReSharper disable once UnusedMember.Global
    internal void Identity_HashCodes_are_Unique()
    {
        var ids = new Dictionary<int, Identity>(75_000_000);
        
        //Identities
        for (var i = 0; i < 25_000; i++)
        {
            //Generations
            for (ushort g = 1; g < 2_000; g++)
            {
                var identity = new Identity(i, g);
                if (ids.ContainsKey(identity.GetHashCode()))
                {
                    Assert.Fail($"Collision of {identity} with {ids[identity.GetHashCode()]}, #{identity.GetHashCode()}");
                }
                else
                {
                    ids.Add(identity.GetHashCode(), identity);
                }
            }
        }
    }

    [Fact]
    public void Identity_Matches_Self_if_Same()
    {
        var random = new Random(420960);
        for (var i = 0; i < 1_000; i++)
        {
            var id = random.Next();
            var gen = (ushort) random.Next();
            
            var self = new Identity(id, gen);
            var other = new Identity(id, gen);

            Assert.Equal(self, other);
        }
    }
}