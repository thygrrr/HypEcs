namespace fennecs;

public partial class World
{
    public QueryBuilder<Entity> Query()
    {
        return new QueryBuilder<Entity>(this);
    }

    public QueryBuilder<C> Query<C>(Entity match = default)
    {
        return new QueryBuilder<C>(this, match);
    }

    public QueryBuilder<C1, C2> Query<C1, C2>(Entity matchAll = default)
    {
        return new QueryBuilder<C1, C2>(this, matchAll, matchAll);
    }

    public QueryBuilder<C1, C2> Query<C1, C2>(Entity match1, Entity match2)
    {
        return new QueryBuilder<C1, C2>(this, match1, match2);
    }

    public QueryBuilder<C1, C2, C3> Query<C1, C2, C3>(Entity matchAll = default)
    {
        return new QueryBuilder<C1, C2, C3>(this, matchAll, matchAll, matchAll);
    }

    public QueryBuilder<C1, C2, C3> Query<C1, C2, C3>(Entity match1, Entity match2, Entity match3)
    {
        return new QueryBuilder<C1, C2, C3>(this, match1, match2, match3);
    }

    public QueryBuilder<C1, C2, C3, C4> Query<C1, C2, C3, C4>()
    {
        return new QueryBuilder<C1, C2, C3, C4>(this);
    }

    public QueryBuilder<C1, C2, C3, C4, C5> Query<C1, C2, C3, C4, C5>()
    {
        return new QueryBuilder<C1, C2, C3, C4, C5>(this);
    }
}