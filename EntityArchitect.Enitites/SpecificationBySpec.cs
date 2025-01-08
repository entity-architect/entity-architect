using System.Linq.Expressions;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Entities.Repository;

namespace EntityArchitect.Entities;

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