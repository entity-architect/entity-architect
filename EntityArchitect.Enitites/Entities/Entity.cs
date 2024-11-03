namespace EntityArchitect.Entities.Entities;

public abstract class Entity : IEntity
{
    public Entity()
    {
        Id = Guid.NewGuid();
    }
    
    public Id<Entity> Id { get; internal set; }
    public DateTime CreatedAt { get; private set;  } = DateTime.Now;
    public DateTime UpdatedAt { get; private set;  }
}
