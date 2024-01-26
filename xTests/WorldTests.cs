namespace xTests;

using ECS;

public class WorldTests(ITestOutputHelper output)
{
    [Fact]
    public World World_Creates()
    {
        var world = new World();
        Assert.NotNull(world);
        return world;
    }

    [Fact]
    public void World_is_Uniquely_Identifiable()
    {
        var ids = new HashSet<Guid> {Guid.Empty};
        for (var i = 0; i < 100; i++)
        {
            using var world = new World();
            Assert.DoesNotContain(world.Info.WorldId, ids);
            ids.Add(world.Info.WorldId);
        }

        Assert.Equal(101, ids.Count);
    }

    [Fact]
    public void World_Disposes()
    {
        using var world = World_Creates();
    }

    [Fact]
    public Entity World_Spawns_valid_Entities()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        Assert.NotEqual(entity, Entity.None);
        Assert.NotEqual(entity, Entity.Any);
        return entity;
    }
}