namespace fennecs.tests;

public class EntityBuilderTests
{
    [Fact]
    public void Cannot_Relate_To_Any()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        var builder = new EntityBuilder(world, entity);
        Assert.Throws<InvalidOperationException>(() => { builder.Add<int>(Identity.Any); });
    }

    [Fact]
    public void Cannot_Relate_To_Any_with_Data()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        var builder = new EntityBuilder(world, entity);
        Assert.Throws<InvalidOperationException>(() => { builder.Add(123, Identity.Any); });
    }
}