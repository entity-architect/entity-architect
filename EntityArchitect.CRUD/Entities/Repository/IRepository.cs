using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityArchitect.CRUD.Entities.Repository;

public interface IRepository<TEntity> where TEntity : Entity
{
    ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
    void Update(TEntity entity);

    Task<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default,
        params string[] includePaths);
    Task<TEntity?> GetByIdAsync(
        Id<TEntity> id,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);
    
    internal Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);
    internal Task<int> GetCountAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}