﻿global using TypeID = short;

namespace fennecs;

internal class LanguageType
{
    protected internal static Type Resolve(TypeID id) => Types[id];
    
    // Shared ID counter
    protected static TypeID Counter;
    
    protected static readonly Dictionary<TypeID, Type> Types = new();
    protected static readonly Dictionary<Type, TypeID> Ids = new();
    
    protected static readonly object RegistryLock = new();

    protected internal static TypeID Identify(Type type)
    {
        lock (RegistryLock)
        {
            // Query the registry directly for a fast response.
            if (Ids.TryGetValue(type, out var id)) return id;
        
            // TODO: Pattern: double-checked locking (DCL); move lock here
            // Query the registry again, this time synchronized.
            //if (Ids.TryGetValue(type, out id)) return id;
            
            // Construct LanguageType<T>, invoking its static constructor.
            Type[] typeArgs = [type];
            var constructed = typeof(LanguageType<>).MakeGenericType(typeArgs);
            constructed.TypeInitializer!.Invoke(null, null);

            // Constructor should have added the type to the registry.
            return Ids[type];
        }
    }

    static LanguageType()
    {
        // Register the None and Any types, blocking off the first and last IDs.
        Types[0] = typeof(None);
        Ids[typeof(None)] = 0;

        Types[TypeID.MaxValue] = typeof(Any);
        Ids[typeof(Any)] = TypeID.MaxValue;
    }

    protected internal struct Any;

    protected internal struct None;
}


internal class LanguageType<T> : LanguageType
{

    // ReSharper disable once StaticMemberInGenericType (we indeed want this unique for each T)
    public static readonly TypeID Id;

    static LanguageType()
    {
        lock (RegistryLock)
        {
            Id = ++Counter;
            if (!Types.TryAdd(Id, typeof(T))) throw new InvalidOperationException($"Type Ids exhausted. {nameof(LanguageType)}() for {nameof(LanguageType<T>)}");
            Ids.Add(typeof(T), Id);
        }
    }

    //FIXME: This collides with certain entity types and generations.
    public static TypeID TargetId => (TypeID) (-Id);
}
