using System.Linq.Expressions;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EntityArchitect.CRUD.Entities.Repository;

public class Repository<TEntity>(ApplicationDbContext context) :
    IRepository<TEntity> where TEntity : Entity
{
    public ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default) => context.Set<TEntity>().AddAsync(entity, cancellationToken);
    public void Remove(TEntity entity) => context.Set<TEntity>().Remove(entity);
    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);
    public Task<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default, params string[] includePaths)
    {
        IQueryable<TEntity> query = context.Set<TEntity>();

        foreach (var path in includePaths)
            query = query.Include(path);
        
        query = query.AsSplitQuery();

        return query.FirstOrDefaultAsync(e => e.Id.Value == id, cancellationToken);
    }

    public ValueTask<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default) => context.Set<TEntity>().FindAsync(new object?[] { id.Value }, cancellationToken: cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => context.Set<TEntity>().AnyAsync(c => c.Id == id, cancellationToken: cancellationToken);

    public Task<TEntity?> GetByIdAsync(
        Id<TEntity> id,
        CancellationToken ct = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = context.Set<TEntity>();

        foreach (var include in includes)
            query = query.Include(include);
        query = query.AsSplitQuery();
        return query.FirstOrDefaultAsync(e => e.Id.Value == id, ct);
    }

    public Task<List<TEntity>> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<TEntity>().AsQueryable();
        foreach (var include in specification.IncludeStrings) query = query.Include(include);
        return query.Where(specification.SpecExpression)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default) => context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    public Task<int> GetCountAsync(CancellationToken cancellationToken) => context.Set<TEntity>().CountAsync(cancellationToken);
}