using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using EntityArchitect.CRUD.Entities.Repository;

namespace EntityArchitect.CRUD.Entities.Entities;

public class SpecificationBySpec<TEntity> : Specification<TEntity> where TEntity : Entity
{
    public SpecificationBySpec(Expression<Func<TEntity, bool>> specExpression, List<string> properties) : base(specExpression)
    {
        foreach (var props in properties)
        {
            IncludeStrings.Add(props);
        }
    }
}