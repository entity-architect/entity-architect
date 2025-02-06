using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Actions;

public abstract class EndpointAction<TEntity>
    where TEntity : Entity
{
    protected internal virtual ValueTask<TEntity> BeforePostAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<TEntity>(entity);
    }

    protected internal virtual ValueTask<TEntity> AfterPostAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<TEntity>(entity);
    }

    protected internal virtual ValueTask<TEntity> BeforePutAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<TEntity>(entity);
    }

    protected internal virtual ValueTask<TEntity> AfterPutAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<TEntity>(entity);
    }

    protected internal virtual ValueTask<TEntity> BeforeDeleteAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<TEntity>(entity);
    }

    protected internal virtual ValueTask<TEntity> AfterDeleteAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<TEntity>(entity);
    }

    protected internal virtual ValueTask<TEntity> AfterGetById(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<TEntity>(entity);
    }

    protected internal virtual ValueTask<List<TEntity>> AfterGetPaginated(int page, int itemCount,
        List<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return new ValueTask<List<TEntity>>(entities);
    }
}