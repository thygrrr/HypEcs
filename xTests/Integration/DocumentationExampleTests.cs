namespace xTests.Integration;
using Position = System.Numerics.Vector3; 

public class DocumentationExampleTests
{
    [Fact]
    private void QuickStart_Example_Works()
    {
        using var world = new World();
        var entity1 = world.Spawn().Add<Position>().Id();
        var entity2 = world.Spawn().Add(new Position(1,2,3)).Add<int>().Id();
        
        var query = world.Query<Position>().Build();

        const float MULTIPLIER = 10f;
        
        query.Run((ref Position pos, float uniform) =>
        {
            pos *= uniform;
        }, MULTIPLIER);

        var pos1 = world.GetComponent<Position>(entity1);
        var expected = new Position() * MULTIPLIER;
        Assert.Equal(expected.X, pos1.X);
        Assert.Equal(expected.Y, pos1.Y);
        Assert.Equal(expected.Z, pos1.Z);

        var pos2 = world.GetComponent<Position>(entity2);
        expected = new Position(1,2,3) * MULTIPLIER;
        Assert.Equal(expected.X, pos2.X);
        Assert.Equal(expected.Y, pos2.Y);
        Assert.Equal(expected.Z, pos2.Z);
    }
}