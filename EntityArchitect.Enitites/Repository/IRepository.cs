using System.Data.Common;
using EntityArchitect.Entities.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityArchitect.Entities.Repository;

public interface IRepository<TEntity> where TEntity : Entity
{
    ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
    void Update(TEntity entity);
    ValueTask<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);
}