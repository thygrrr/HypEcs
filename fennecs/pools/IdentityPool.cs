namespace fennecs.pools;

public class IdentityPool(int capacity = 4096)
{
    public int Living { get; private set; }
    public int Count => Living - _recycled.Count;

    private readonly Queue<Identity> _recycled = new(capacity);

    public Identity Spawn()
    {
        if (_recycled.TryDequeue(out var recycledIdentity)) return recycledIdentity;
        
        return new Identity(++Living);
    }

    public void Despawn(Identity identity)
    {
        _recycled.Enqueue(identity.Successor);
    }
}


public class ReferenceStore<T>(int capacity = 4096) where T : class
{
    private readonly IdentityPool _pool = new();

    private Identity[] _keys = new Identity[capacity];
    private WeakReference<T>[] _storage = new WeakReference<T>[capacity];
    private Dictionary<object, Identity> _mapping = new(capacity);
    
    public Identity Spawn(T item)
    {
        var identity = _pool.Spawn();

        // For Collect() method, we need a way to reconstruct the identity from the values.
        if (_pool.Living >= _keys.Length)
        {
            Array.Resize(ref _keys, _pool.Living);
            Array.Resize(ref _storage, _pool.Living);
        }
        
        _keys[identity.Id] = identity;
        _storage[identity.Id] = new WeakReference<T>(item);
        
        return identity;
    }

    public void Despawn(Identity identity)
    {
        _keys[identity.Id] = default;
        _storage[identity.Id] = default!;
        _pool.Despawn(identity);
    }

    public IEnumerable<Identity> Keys()
    {
        return _keys.Where(identity => identity != default);
    }

    public void Collect()
    {
        for (var i = 0; i < _keys.Length; i++)
        {
            if (_keys[i] == default) continue;
            if (_storage[i].TryGetTarget(out _) == false) Despawn(_keys[i]);
        }
    }

    public T this[Identity identity]
    {
        get
        {
            if (_storage[identity.Id]!.TryGetTarget(out var item)) return item;
            throw new KeyNotFoundException();
        }
    }
}