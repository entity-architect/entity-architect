using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityArchitect.CRUD.Entities.Repository;

public interface IRepository<TEntity> where TEntity : Entity
{
    ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
    void Update(TEntity entity);
    Task<TEntity?> GetByIdAsync(Id<TEntity> id, List<string>? includeProperties = null,
        CancellationToken cancellationToken = default);
    Task<TEntity?> GetBySpecificationIdAsync(SpecificationBySpec<TEntity> specification,
        CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetLightListAsync(CancellationToken cancellationToken);
    Task<List<TEntity>> GetAllPaginatedAsync(int page, int itemCount, List<string> includingProperties,
        CancellationToken cancellationToken);
    Task<int> GetCountAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}