using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Actions;

public abstract class EndpointAction<TEntity>
    where TEntity : Entity
{
    protected internal virtual ValueTask<Result<TEntity>> BeforePostAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<TEntity>>(entity);
    }

    protected internal virtual ValueTask<Result<TEntity>> AfterPostAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<TEntity>>(entity);
    }

    protected internal virtual ValueTask<Result<TEntity>> BeforePutAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<TEntity>>(entity);
    }

    protected internal virtual ValueTask<Result<TEntity>> AfterPutAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<TEntity>>(entity);
    }

    protected internal virtual ValueTask<Result<TEntity>> BeforeDeleteAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<TEntity>>(entity);
    }

    protected internal virtual ValueTask<Result<TEntity>> AfterDeleteAsync(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<TEntity>>(entity);
    }

    protected internal virtual ValueTask<Result<TEntity>> AfterGetById(TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<TEntity>>(entity);
    }

    protected internal virtual ValueTask<Result<List<TEntity>>> AfterGetPaginated(int page, int itemCount,
        List<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return new ValueTask<Result<List<TEntity>>>(entities);
    }
}