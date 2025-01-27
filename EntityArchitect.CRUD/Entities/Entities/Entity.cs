namespace EntityArchitect.CRUD.Entities.Entities;

public abstract class Entity : IEntity
{
    public Entity()
    {
        Id = Guid.NewGuid();
    }

    public Id<Entity> Id { get; internal set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; }

    public void SetCreatedDate()
    {
        CreatedAt = DateTime.Now;
    }
}