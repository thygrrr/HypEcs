namespace fennecs.tests.Integration;
using Position = System.Numerics.Vector3;

public class DocumentationExampleTests
{
    [Fact]
    private void QuickStart_Example_Works()
    {
        using var world = new World();
        var entity1 = world.Spawn().Add<Position>().Id();
        var entity2 = world.Spawn().Add(new Position(1, 2, 3)).Add<int>().Id();

        var query = world.Query<Position>().Build();

        const float MULTIPLIER = 10f;

        query.RunParallel((ref Position pos, float uniform) => { pos *= uniform; }, MULTIPLIER, chunkSize: 4000);

        var pos1 = world.GetComponent<Position>(entity1);
        var expected = new Position() * MULTIPLIER;
        Assert.Equal(expected, pos1);

        var pos2 = world.GetComponent<Position>(entity2);
        expected = new Position(1, 2, 3) * MULTIPLIER;
        Assert.Equal(expected, pos2);
    }

    [Fact]
    private void Has_Matches()
    {
        var p1 = new Position(6, 6, 6);
        var p2 = new Position(1, 2, 3);

        using var world = new World();
        world.Spawn().Add(p1).Id();
        world.Spawn().Add(p2).Add<int>().Id();
        world.Spawn().Add(p2).Add<int>().Id();

        var query = world.Query<Position>().Has<int>().Build();

        query.Raw(memory =>
        {
            Assert.True(memory.Length == 2);
            foreach (var pos in memory.Span) Assert.Equal(p2, pos);
        });
    }

    [Fact]
    private void Not_prevents_Match()
    {
        var p1 = new Position(6, 6, 6);
        var p2 = new Position(1, 2, 3);

        using var world = new World();
        world.Spawn().Add(p1).Id();
        world.Spawn().Add(p2).Add<int>().Id();
        world.Spawn().Add(p2).Add<int>().Id();

        var query = world.Query<Position>().Not<int>().Build();

        query.Raw(memory =>
        {
            Assert.True(memory.Length == 1);
            foreach (var pos in memory.Span) Assert.Equal(p1, pos);
        });
    }

    [Fact]
    private void Any_Target_None_Matches_Only_None()
    {
        var p1 = new Position(6, 6, 6);
        var p2 = new Position(1, 2, 3);
        var p3 = new Position(4, 4, 4);

        using var world = new World();
        var e1 = world.Spawn().Add(p1).Add(0).Id();
        var e2 = world.Spawn().Add(p2).Add(e1, 111).Id();
        var e3 = world.Spawn().Add(p3).Add(e2, 222).Id();

        var query = world.Query<Entity, Position>().Any<int>().Build();

        query.Raw((me, mp) =>
        {
            Assert.True(mp.Length == 1);
            var ex = me.Span[0];
            Assert.Equal(e1, ex);
//          if (me.Span[0] == e2) Assert.True(mp.Span.Contains(p2));
//          if (me.Span[0] == e3) Assert.True(mp.Span.Contains(p3));
        });
    }
}