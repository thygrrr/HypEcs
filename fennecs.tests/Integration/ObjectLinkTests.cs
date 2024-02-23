namespace fennecs.tests.Integration;

public class ObjectLinkTests(ITestOutputHelper output)
{
    [Fact]
    public void Can_Link_Objects()
    {
        using var world = new World();
        using var query = world.Query<string>(Entity.Any).Build();

        world.Spawn().AddLink("hello world");

        var runs = 0;
        query.ForEach((ref string str) =>
        {
            runs++;
            output.WriteLine(str);            
            Assert.Equal("hello world", str);
        });
        Assert.Equal(1, runs);
    }
}