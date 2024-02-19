using System.Collections.Concurrent;
using fennecs.pools;

namespace fennecs;

internal class LanguageType
{
    protected internal static Type Resolve(ushort id) => Types[id];

    // ReSharper disable once StaticMemberInGenericType
    protected static ushort Counter;
    protected static readonly Dictionary<ushort, Type> Types = new();
    protected static readonly Dictionary<Type, ushort> Ids = new();
    
    protected static readonly object RegistryLock = new();

    protected internal static ushort Identify(Type type)
    {
        lock (RegistryLock) // Maybe there's a nicer/safer way for this?
        {
            // Query the registry directly.
            if (Ids.TryGetValue(type, out var id)) return id;

            // Construct LanguageType<T> and invoke static constructor.
            Type[] typeArgs = [type];
            var constructed = typeof(LanguageType<>).MakeGenericType(typeArgs);
            constructed.TypeInitializer?.Invoke(null, null);

            // Constructor should have added the type to the registry.
            return Ids[type];
        }
    }

    static LanguageType()
    {
        Types[0] = typeof(None);
        Ids[typeof(None)] = 0;

        Types[ushort.MaxValue] = typeof(Any);
        Ids[typeof(Any)] = ushort.MaxValue;
    }

    protected internal struct Any;

    protected internal struct None;
}


internal class LanguageType<T> : LanguageType
{
    // ReSharper disable once StaticMemberInGenericType (we want this unique for each T)
    public static readonly ushort Id;

    static LanguageType()
    {
        lock (RegistryLock)
        {
            Id = ++Counter;
            Types.Add(Id, typeof(T));
            Ids.Add(typeof(T), Id);
        }
    }
}

internal class RelationType<T> : LanguageType where T : class
{
    // ReSharper disable once StaticMemberInGenericType (we want this unique for each T)
    private static ReferenceStore<T> _store = new();

    // ReSharper disable once StaticMemberInGenericType (we want this unique for each T)
    public static readonly ushort Id;

    static RelationType()
    {
        lock (RegistryLock)
        {
            Id = ++Counter;
            Types.Add(Id, typeof(T));
            Ids.Add(typeof(T), Id);
        }
    }
}
