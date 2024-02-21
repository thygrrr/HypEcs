// SPDX-License-Identifier: MIT

namespace fennecs.tests;

public class EntityTests(ITestOutputHelper output)
{
    [Fact]
    public void Virtual_Entities_have_no_Successors()
    {
        Assert.Throws<InvalidCastException>(() => Entity.Any.Successor);
        Assert.Throws<InvalidCastException>(() => Entity.None.Successor);
        Assert.Throws<InvalidCastException>(() => new Entity(typeof(bool)).Successor);
    }

    [Fact]
    public void NonVirtual_Identity_Resolves_as_Type()
    {
        var boolType = new Entity(typeof(bool));
        Assert.Equal(typeof(bool), boolType.Type);

        Assert.Equal(typeof(LanguageType.Any), Entity.Any.Type);
        Assert.Equal(typeof(LanguageType.None), Entity.None.Type);

        using var world = new World();
        var identity = world.Spawn().Id();
        Assert.Equal(typeof(Entity), identity.Type);
    }
    
    [Fact]
    public void Identity_None_is_Zeros()
    {
        var none = Entity.None;
        Assert.Equal(default, none.Generation);
        output.WriteLine(none.Generation.ToString());
        output.WriteLine(none.ToString());
        Assert.Equal(default, none.Id);
    }

    [Fact]
    public void Identity_ToString()
    {
        output.WriteLine(Entity.None.ToString());
        output.WriteLine(Entity.Any.ToString());
        output.WriteLine(new Entity(123).ToString());
    }

    [Fact]
    public void Identity_None_cannot_Match_One()
    {
        var zero = new Entity(0);
        Assert.NotEqual(Entity.None, zero);

        var one = new Entity(1);
        Assert.NotEqual(Entity.None, one);
    }

    [Fact]
    public void Identity_Matches_Only_Self()
    {
        var self = new Entity(12345);
        Assert.Equal(self, self);

        var successor = new Entity(12345, 3);
        Assert.NotEqual(self, successor);

        var other = new Entity(9000, 3);
        Assert.NotEqual(self, other);

    }

    [Theory]
    [InlineData(1500, 1500)]
    internal void Identity_HashCodes_are_Unique(TypeID idCount, TypeID genCount)
    {
        var ids = new Dictionary<int, Entity>((int) (idCount * genCount * 4f));

        //Identities
        for (var i = 0; i < idCount ; i++)
        {
            //Generations
            for (TypeID g = 1; g < genCount; g++)
            {
                var identity = new Entity(i, g);

                Assert.NotEqual(identity, Entity.Any);
                Assert.NotEqual(identity, Entity.None);

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
    public void Equals_Prevents_Boxing_as_InvalidCastException()
    {
        object o = "don't @ me";
        var id = new Entity(69, 420);
        Assert.Throws<InvalidCastException>(() => id.Equals(o));
    }

    [Fact]
    public void Any_and_None_are_Distinct()
    {
        Assert.NotEqual(Entity.Any, Entity.None);
        Assert.NotEqual(Entity.Any.GetHashCode(), Entity.None.GetHashCode());
    }

    [Fact]
    public void Identity_Matches_Self_if_Same()
    {
        var random = new Random(420960);
        for (var i = 0; i < 1_000; i++)
        {
            var id = random.Next();
            var gen = (TypeID) random.Next();
            
            var self = new Entity(id, gen);
            var other = new Entity(id, gen);

            Assert.Equal(self, other);
        }
    }

    [Fact]
    private void Implicit_Cast_From_Type()
    {
        var type = typeof(int);
        Entity entity= type;
        Assert.Equal(type, entity.Type);
    }
}