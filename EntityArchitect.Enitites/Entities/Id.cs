namespace EntityArchitect.Entities.Entities;

public class Id<TEntity> : IEquatable<Id<TEntity>> where TEntity : Entity
{
    public Guid Value { get; }

    public Id(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Guid cannot be empty.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(Id<TEntity> id) => id.Value;
    public static implicit operator Id<TEntity>(Guid value) => new Id<TEntity>(value);

    public Id<Entity> ToId() => new Id<Entity>(this);

    public override bool Equals(object? obj) => obj is Id<TEntity> id && Value.Equals(id.Value);

    public bool Equals(Id<TEntity>? other) => other is not null && Value.Equals(other.Value);

    public override int GetHashCode() => Value.GetHashCode();
}