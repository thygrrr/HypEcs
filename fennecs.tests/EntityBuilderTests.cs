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

    [Fact(Skip = "Refactor")]
    public void Can_Remove_Type_Target()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        var builder = new EntityBuilder(world, entity);
        builder.Add(123, typeof(int));
        builder.Remove<int>(typeof(int));
        Assert.False(world.HasComponent<int>(entity));
    }


    struct Owes
    {
        public int amount;
    }

    [Fact(Skip = "Refactor")]
    public void Can_Add_Type_as_Relation_Target()
    {
        using var world = new World();
        var entity = world.Spawn().Id();
        
        world.On(entity).Add(new Owes {amount = 123}, typeof(EntityBuilderTests));
        
        var query = world.Query<Owes>().Has<Owes>(typeof(EntityBuilderTests)).Build();
        var owes = query.Ref(entity);
        Assert.Equal(123, owes.amount);
    }
}