using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Dapper;
using EFCore.NamingConventions.Internal;
using Microsoft.AspNetCore.Html;

namespace EntityArchitect.CRUD.Entities.Repository;

public class Repository<TEntity>(ApplicationDbContext context) :
    IRepository<TEntity> where TEntity : Entity
{
    public ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return context.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Remove(TEntity entity)
    {
        context.Set<TEntity>().Remove(entity);
    }

    public void Update(TEntity entity)
    {
        context.Set<TEntity>().Update(entity);
    }

    public ValueTask<TEntity?> GetByIdAsync(Id<TEntity> id, CancellationToken cancellationToken = default)
    {
        return context.Set<TEntity>().FindAsync(new object[] { id.ToId() }, cancellationToken);
    }

    public Task<TEntity?> GetBySpecificationIdAsync(SpecificationBySpec<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<TEntity>().AsQueryable();
        foreach (var include in specification.IncludeStrings) query = query.Include(include);
        return query.FirstOrDefaultAsync(specification.SpecExpression, cancellationToken);
    }

    public Task<List<TEntity>> GetBySpecificationAsync(ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<TEntity>().AsQueryable();
        foreach (var include in specification.IncludeStrings) query = query.Include(include);
        return query.Where(specification.SpecExpression)
            .ToListAsync(cancellationToken);
    }
    
    public Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default) =>
        context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

    public Task<List<TEntity>> GetLightListAsync(CancellationToken cancellationToken)
    {
        return context.Set<TEntity>().ToListAsync(cancellationToken);
    }

    public Task<List<TEntity>> GetAllPaginatedAsync(int page, int itemCount, List<string> includingProperties,
        CancellationToken cancellationToken)
    {
        var query = context.Set<TEntity>().AsQueryable();
        foreach (var include in includingProperties) query = query.Include(include);
        return query.Skip(page * itemCount)
            .Take(itemCount)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        return context.Set<TEntity>().CountAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => 
        context.Set<TEntity>().AnyAsync(c => c.Id == id, cancellationToken: cancellationToken);
}