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

    public Task<TEntity?> GetBySpecificationIdAsync(SpecificationGetById<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<TEntity>().AsQueryable();
        foreach (var include in specification.IncludeStrings)
        {
            query = query.Include(include);
        }
        return query.FirstOrDefaultAsync(specification.SpecExpression, cancellationToken);
    }

    public Task<List<TEntity>> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<TEntity>().AsQueryable();
        foreach (var include in specification.IncludeStrings)
        {
            query = query.Include(include);
        }
        return query.Where(specification.SpecExpression)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

    public Task<List<TEntity>> GetLightListAsync(CancellationToken cancellationToken) => 
        context.Set<TEntity>().ToListAsync(cancellationToken);

    public Task<List<TEntity>> GetAllPaginatedAsync(int page, int itemCount, List<string> includingProperties, CancellationToken cancellationToken)
    {
        var query = context.Set<TEntity>().AsQueryable();
        foreach (var include in includingProperties)
        {
            query = query.Include(include);
        }
        return query.Skip(page * itemCount)
            .Take(itemCount)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken) => 
        context.Set<TEntity>().CountAsync(cancellationToken);
}