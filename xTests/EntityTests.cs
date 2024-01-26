namespace xTests;

public class EntityTests(ITestOutputHelper output)
{
    [Fact]
    public Entity Entity_is_Alive_after_Spawn()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        Assert.True(world.IsAlive(entity));
        return entity;
    }

    [Fact]
    private void Entity_is_Not_Alive_after_Despawn()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        world.Despawn(entity);
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    private void Entity_has_no_Components_after_Spawn()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        var components = world.GetComponents(entity);
        Assert.False(world.HasComponent<int>(entity));
        Assert.True(components.Count() == 1);
    }

    [Fact]
    private void Entity_can_Add_Component()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        world.On(entity).Add<int>();
        Assert.True(world.HasComponent<int>(entity));
        var components = world.GetComponents(entity);
        Assert.True(components.Count() == 2);
    }

    [Fact]
    private void Entity_can_Remove_Component()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        world.On(entity).Add<int>();
        world.On(entity).Remove<int>();
        Assert.False(world.HasComponent<int>(entity));
    }

    [Fact]
    private void Entity_can_ReAdd_Component()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        world.On(entity).Add<int>(123);
        world.On(entity).Remove<int>();
        world.On(entity).Add<int>(345);
        Assert.True(world.HasComponent<int>(entity));
    }

    [Fact]
    private void Entity_cannot_Add_Component_twice()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        world.On(entity).Add<int>();
        Assert.Throws<ArgumentException>(() => world.On(entity).Add<int>());
    }

    [Fact]
    private void Entity_cannot_Remove_Component_twice()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        world.On(entity).Add<int>();
        world.On(entity).Remove<int>();
        Assert.Throws<ArgumentException>(() => world.On(entity).Remove<int>());
    }

    [Fact]
    private void Entity_cannot_Remove_Component_without_Adding()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        Assert.Throws<ArgumentException>(() => world.On(entity).Remove<int>());
    }
}