using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityArchitect.CRUD.Entities.Repository;

public interface IRepository<TEntity> where TEntity : Entity
{
    ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
    void Update(TEntity entity);
    ValueTask<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetBySpecificationIdAsync(SpecificationBySpec<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetLightListAsync(CancellationToken cancellationToken);

    Task<List<TEntity>> GetAllPaginatedAsync(int page, int itemCount, List<string> includingProperties,
        CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);
}