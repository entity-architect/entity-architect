using System;

namespace EntityArchitect.CRUD.Entities.Entities;

public class Id<TEntity> : IEquatable<Id<TEntity>> where TEntity : Entity
{
    public Id(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Guid cannot be empty.", nameof(value));
        Value = value;
    }

    public Guid Value { get; }

    public bool Equals(Id<TEntity>? other)
    {
        return other is not null && Value.Equals(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(Id<TEntity> id)
    {
        return id.Value;
    }

    public static implicit operator Id<TEntity>(Guid value)
    {
        return new Id<TEntity>(value);
    }

    public Id<Entity> ToId()
    {
        return new Id<Entity>(this);
    }
    
  

    public override bool Equals(object? obj)
    {
        return obj is Id<TEntity> id && Value.Equals(id.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

public static class IdExtensions
{
    public static Id<TEntity> ToId<TEntity>(this Guid guid) where TEntity : Entity
    {
        return new Id<TEntity>(guid);
    }
}