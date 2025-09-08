using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityArchitect.CRUD.Entities;

public class SpecificationGetById<TEntity> : Specification<TEntity> where TEntity : Entity
{
    public SpecificationGetById(Expression<Func<TEntity, bool>> specExpression, List<string> properties) : base(
        specExpression)
    {
        foreach (var props in properties) IncludeStrings.Add(props);
    }
    

}

public class SpecificationWithInclude<TEntity> : Specification<TEntity>
    where TEntity : Entity
{
    private readonly List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>> _includeChains = [];

    public List<Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>> IncludeChains => _includeChains;

    public SpecificationWithInclude(
        Expression<Func<TEntity, bool>> criteria,
        params Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>[]? includeChains
    ) : base(criteria)
    {
        if (includeChains != null)
        {
            IncludeChains.AddRange(includeChains);
        }
    }
}
