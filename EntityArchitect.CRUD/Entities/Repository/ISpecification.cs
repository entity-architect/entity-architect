using System.Linq.Expressions;

namespace EntityArchitect.CRUD.Entities.Repository;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> SpecExpression { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    bool IsSatisfiedBy(T obj);
}