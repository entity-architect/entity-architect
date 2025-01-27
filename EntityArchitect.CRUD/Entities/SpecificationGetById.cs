using System.Linq.Expressions;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;

namespace EntityArchitect.CRUD.Entities;

public class SpecificationGetById<TEntity> : Specification<TEntity> where TEntity : Entity
{
    public SpecificationGetById(Expression<Func<TEntity, bool>> specExpression, List<string> properties) : base(
        specExpression)
    {
        foreach (var props in properties) IncludeStrings.Add(props);
    }
}