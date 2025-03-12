using System;

namespace EntityArchitect.CRUD.Entities.Entities;

public abstract class Entity : IEntity
{
    public Entity()
    {
        Id = Guid.NewGuid();
        SetCreatedDate();
    }

    public Id<Entity> Id { get; internal set; }
    public DateTime CreatedAt { get; internal set; }
    public DateTime UpdatedAt { get; internal set; }

    public void SetCreatedDate()
    {
        CreatedAt = DateTime.Now.ToUniversalTime();
    }
    
    public static TEntity CreateFromId<TEntity>(Guid id) where TEntity : Entity
    {
        var entity = (TEntity)Activator.CreateInstance(typeof(TEntity))!;
        entity.Id = id;
        return entity;
    }
}