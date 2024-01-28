namespace ECS;

public sealed class Mask
{
    internal readonly List<TypeExpression> HasTypes = [];
    internal readonly List<TypeExpression> NotTypes = [];
    internal readonly List<TypeExpression> AnyTypes = [];

    
    public void Has(TypeExpression type_expression)
    {
        HasTypes.Add(type_expression);
    }

    
    public void Not(TypeExpression type_expression)
    {
        NotTypes.Add(type_expression);
    }

    
    public void Any(TypeExpression type_expression)
    {
        AnyTypes.Add(type_expression);
    }

    public void Clear()
    {
        HasTypes.Clear();
        NotTypes.Clear();
        AnyTypes.Clear();
    }

    
    public override int GetHashCode()
    {
        var hash = HasTypes.Count + AnyTypes.Count + NotTypes.Count;

        unchecked
        {
            foreach (var type in HasTypes) hash = hash * 314159 + type.Value.GetHashCode();
            foreach (var type in NotTypes) hash = hash * 314159 - type.Value.GetHashCode();
            foreach (var type in AnyTypes) hash *= 314159 * type.Value.GetHashCode();
        }

        return hash;
    }
}