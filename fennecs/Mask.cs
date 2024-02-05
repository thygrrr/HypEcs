// SPDX-License-Identifier: MIT

namespace fennecs;

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

        return HasTypes.SequenceEqual(other.HasTypes)
            && NotTypes.SequenceEqual(other.NotTypes)
            && AnyTypes.SequenceEqual(other.AnyTypes);
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
