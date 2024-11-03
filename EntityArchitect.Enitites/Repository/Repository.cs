using EntityArchitect.Entities.Context;
using EntityArchitect.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityArchitect.Entities.Repository;

public class Repository<TEntity>(ApplicationDbContext context) :
    IRepository<TEntity> where TEntity : Entity
{
    public ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().AddAsync(entity, cancellationToken);

    public void Remove(TEntity entity) =>
        context.Set<TEntity>().Remove(entity);

    public void Update(TEntity entity) =>
        context.Set<TEntity>().Update(entity);

    public ValueTask<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().FindAsync(new object[] { id.ToId() }, cancellationToken);

    public Task<TEntity?> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        context.Set<TEntity>().FirstOrDefaultAsync(specification.SpecExpression, cancellationToken);

    public Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
}