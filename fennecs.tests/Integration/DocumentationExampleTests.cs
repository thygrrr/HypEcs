namespace fennecs.tests.Integration;
using Position = System.Numerics.Vector3;

public class DocumentationExampleTests
{
    [Fact]
    public void QuickStart_Example_Works()
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

        var query = world.Query<Position>()
            .Has<int>()
            .Build();

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

        var query = world.Query<Position>()
            .Not<int>()
            .Build();

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
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var bob = world.Spawn().Add(p2).Add(alice, 111).Id();
        /*var charlie = */world.Spawn().Add(p3).Add(bob, 222).Id();

        var query = world.Query<Entity, Position>()
            .Any<int>(Identity.None)
            .Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            count++;
            Assert.Equal(1, mp.Length);
            var entity = me.Span[0];
            Assert.Equal(alice, entity);
        });
        Assert.Equal(1, count);
    }

    [Fact]
    private void Any_Target_Single_Matches()
    {
        var p1 = new Position(6, 6, 6);
        var p2 = new Position(1, 2, 3);
        var p3 = new Position(4, 4, 4);

        using var world = new World();
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var eve = world.Spawn().Add(p2).Add(alice, 111).Id();
        var charlie = world.Spawn().Add(p3).Add(eve, 222).Id();

        var query = world.Query<Entity, Position>().Any<int>(eve).Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            count++;
            Assert.Equal(1, mp.Length);
            var entity = me.Span[0];
            Assert.Equal(charlie, entity);
            var pos = mp.Span[0];
            Assert.Equal(pos, p3);
        });
        Assert.Equal(1, count);
    }

    [Fact]
    private void Any_Target_Multiple_Matches()
    {
        var p1 = new Position(6, 6, 6);
        var p2 = new Position(1, 2, 3);
        var p3 = new Position(4, 4, 4);

        using var world = new World();
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var eve = world.Spawn().Add(p2).Add(alice, 111).Id();
        var charlie = world.Spawn().Add(p3).Add(eve, 222).Id();

        var query = world.Query<Entity, Position>()
            .Any<int>(eve)
            .Any<int>(alice)
            .Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            Assert.Equal(1, mp.Length);
            for (var index = 0; index < me.Length; index++)
            {
                var entity = me.Span[index];
                count++;
                if (entity == charlie)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p3);
                }
                else if (entity == eve)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p2);
                }
                else
                {
                    Assert.Fail("Unexpected entity");
                }
            }
        });
        Assert.Equal(2, count);
    }

    [Fact]
    private void Any_Not_does_not_Match_Specific()
    {
        var p1 = new Position(6, 6, 6);
        var p2 = new Position(1, 2, 3);
        var p3 = new Position(4, 4, 4);

        using var world = new World();
        var alice = world.Spawn().Add(p1).Add(0).Id();
        var bob = world.Spawn().Add(p2).Add(alice, 111).Id();
        var eve = world.Spawn().Add(p1).Add(888).Id();

        /*var charlie = */
        world.Spawn().Add(p3).Add(bob, 222).Id();
        /*var charlie = */
        world.Spawn().Add(p3).Add(eve, 222).Id();

        var query = world.Query<Entity, Position>()
            .Not<int>(bob)
            .Any<int>(alice)
            .Build();

        var count = 0;
        query.Raw((me, mp) =>
        {
            Assert.Equal(1, mp.Length);
            for (var index = 0; index < me.Length; index++)
            {
                var entity = me.Span[index];
                count++;
                if (entity == bob)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p2);
                }
                else
                {
                    Assert.Fail("Unexpected entity");
                }
            }
        });
        Assert.Equal(1, count);
    }

    [Fact]
    private void Query_provided_Has_works_with_Target()
    {
        var p1 = new Position(6, 6, 6);
        var p2 = new Position(1, 2, 3);
        var p3 = new Position(4, 4, 4);

        using var world = new World();

        var alice = world.Spawn().Add(p1).Add(0).Id();
        var eve = world.Spawn().Add(p1).Add(888).Id();

        var bob = world.Spawn().Add(p2).Add(alice, 111).Id();

        world.Spawn().Add(p3).Add(bob, 555).Id();
        world.Spawn().Add(p3).Add(eve, 666).Id();

        var query = world.Query<Entity, Position, int>()
            .Not<int>(bob)
            .Build();

        var count = 0;
        query.Raw((me, mp, mi) =>
        {
            Assert.Equal(2, mp.Length);
            for (var index = 0; index < me.Length; index++)
            {
                var entity = me.Span[index];
                count++;
                
                if (entity == alice)
                {
                    var pos = mp.Span[0];
                    Assert.Equal(pos, p1);
                    var integer = mi.Span[index];
                    Assert.Equal(0, integer);
                }
                else if (entity == eve)
                {
                    var pos = mp.Span[index];
                    Assert.Equal(pos, p1);
                    var i = mi.Span[index];
                    Assert.Equal(888, i);
                }
                else
                {
                    Assert.Fail($"Unexpected entity {entity}");
                }
            }
        });
        Assert.Equal(2, count);
    }
    
}