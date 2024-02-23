namespace fennecs.tests.Integration;

public class WildcardTests
{
    [Fact]
    public void Wildcard_Any_Enumerates_all_Components_Once()
    {
        using var world = new World();
        using var query = world.Query<string>(Entity.Any).Build();

        // string may be interned or not
        const string OBJECT1 = "hello world";
        const string OBJECT2 = "fly, you fools";
        const string NONE1 = "can't touch this";
        const string RELATION1 = "IOU";

        var bob = world.Spawn().Id();
        world.Spawn().AddLink(OBJECT1).AddLink(OBJECT2).Add(NONE1).AddRelation(bob, RELATION1).Id();

        HashSet<string> seen = [];
        query.ForEach((ref string str) =>
        {
            Assert.DoesNotContain(str, seen);
            seen.Add(str);
        });
        Assert.Contains(OBJECT1, seen);
        Assert.Contains(OBJECT2, seen);
        Assert.Contains(NONE1, seen);
        Assert.Contains(RELATION1, seen);
        Assert.Equal(4, seen.Count);
    }
    

    [Fact]
    public void Wildcard_None_Enumerates_Only_Plain_Components()
    {
        using var world = new World();
        using var query = world.Query<string>(Entity.None).Build();

        // string may be interned or not
        const string TARGET1 = "hello world";
        const string TARGET2 = "fly, you fools";
        const string TARGET3 = "can't touch this";
        const string TARGET4 = "IOU";

        var bob = world.Spawn().Id();
        world.Spawn().AddLink(TARGET1).AddLink(TARGET2).Add(TARGET3).AddRelation(bob, TARGET4).Id();

        HashSet<string> seen = [];
        query.ForEach((ref string str) =>
        {
            Assert.DoesNotContain(str, seen);
            seen.Add(str);
        });
        Assert.Contains(TARGET3, seen);
        Assert.Single(seen);
    }


    [Fact]
    public void Wildcard_Target_Enumerates_all_Relations()
    {
        using var world = new World();
        using var query = world.Query<string>(Entity.Target).Build();

        // string may be interned or not
        const string OBJECT1 = "hello world";
        const string OBJECT2 = "fly, you fools";
        const string NONE1 = "can't touch this";
        const string RELATION1 = "IOU";

        var bob = world.Spawn().Id();
        world.Spawn().AddLink(OBJECT1).AddLink(OBJECT2).Add(NONE1).AddRelation(bob, RELATION1).Id();

        HashSet<string> seen = [];

        query.ForEach((ref string str) =>
        {
            Assert.DoesNotContain(str, seen);
            seen.Add(str);
        });
        Assert.Contains(OBJECT1, seen);
        Assert.Contains(OBJECT2, seen);
        Assert.Contains(RELATION1, seen);
        Assert.Equal(3, seen.Count);
    }



    [Fact]
    public void Wildcard_Relation_Enumerates_all_Relations()
    {
        using var world = new World();
        using var query = world.Query<string>(Entity.Relation).Build();

        // string may be interned or not
        const string OBJECT1 = "hello world";
        const string OBJECT2 = "fly, you fools";
        const string NONE1 = "can't touch this";
        const string RELATION1 = "IOU";

        var bob = world.Spawn().Id();
        world.Spawn().AddLink(OBJECT1).AddLink(OBJECT2).Add(NONE1).AddRelation(bob, RELATION1).Id();

        HashSet<string> seen = [];

        query.ForEach((ref string str) =>
        {
            Assert.DoesNotContain(str, seen);
            seen.Add(str);
        });
        Assert.Contains(RELATION1, seen);
        Assert.Single(seen);
    }



    [Fact]
    public void Wildcard_Object_Enumerates_all_Object_Links()
    {
        using var world = new World();
        using var query = world.Query<string>(Entity.Object).Build();

        // string may be interned or not
        const string OBJECT1 = "hello world";
        const string OBJECT2 = "fly, you fools";
        const string NONE1 = "can't touch this";
        const string RELATION1 = "IOU";

        var bob = world.Spawn().Id();
        world.Spawn().AddLink(OBJECT1).AddLink(OBJECT2).Add(NONE1).AddRelation(bob, RELATION1).Id();

        var runs = 0;
        HashSet<string> seen = [];

        query.ForEach((ref string str) =>
        {
            runs++;
            Assert.DoesNotContain(str, seen);
            seen.Add(str);
            Assert.True(ReferenceEquals(OBJECT1, str) || ReferenceEquals(OBJECT2, str));
        });
        Assert.Equal(2, runs);
    }
}