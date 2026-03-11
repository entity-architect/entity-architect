using System.Linq.Expressions;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Memory;

namespace EntityArchitect.CRUD.Entities.Repository;

public interface IRepository<TEntity> where TEntity : Entity
{
    ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
    void Update(TEntity entity);

    Task<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default,
        params string[] includePaths);    
    ValueTask<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(
        Id<TEntity> id,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes);

    internal Task<List<TEntity>> GetBySpecificationAsync(ISpecification<TEntity> spec,
        CancellationToken cancellationToken = default);
    internal Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);
    internal Task<int> GetCountAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}