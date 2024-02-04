// SPDX-License-Identifier: MIT

namespace ECS;

public sealed class Mask : IEquatable<Mask>, IDisposable
{
    internal readonly List<TypeExpression> HasTypes = [];
    internal readonly List<TypeExpression> NotTypes = [];
    internal readonly List<TypeExpression> AnyTypes = [];
    
    public void Has(TypeExpression type)
    {
        HasTypes.Add(type);
    }
    
    public void Not(TypeExpression type)
    {
        NotTypes.Add(type);
    }

    
    public void Any(TypeExpression type)
    {
        if (type.Identity == Identity.Any) throw new InvalidOperationException("Mask.Any: Can't have Any type as Any");
        if (type.Identity == Identity.None) throw new InvalidOperationException("Mask.Any: Can't have None type as Any");
        AnyTypes.Add(type);
    }

    public void Clear()
    {
        HasTypes.Clear();
        NotTypes.Clear();
        AnyTypes.Clear();
    }


    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return HashCode.Combine(HasTypes, NotTypes, AnyTypes);
    }

    public static implicit operator int(Mask self)
    {
        return self.GetHashCode();
    }

    public bool Equals(Mask? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return HasTypes.Equals(other.HasTypes) && NotTypes.Equals(other.NotTypes) && AnyTypes.Equals(other.AnyTypes);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Mask other && Equals(other);
    }

    public void Dispose()
    {
        Clear();
    }
}
