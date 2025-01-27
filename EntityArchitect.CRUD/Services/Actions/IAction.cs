using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Services.Actions;

public interface IAction<TEntity>
    where TEntity : Entity
{
    ValueTask<TEntity> InvokeAsync(TEntity entity, CancellationToken cancellationToken);
}