using System.Linq.Expressions;

namespace EntityArchitect.CRUD.Entities.Repository;

public abstract class Specification<T> : ISpecification<T>
{
    private Func<T, bool>? _compiledExpression;

    public Specification(Expression<Func<T, bool>> specExpression)
    {
        SpecExpression = specExpression;
    }

    public Expression<Func<T, bool>> SpecExpression { get; }

    public List<Expression<Func<T, object>>> Includes { get; } = new();

    public List<string> IncludeStrings { get; } = new();

    public bool IsSatisfiedBy(T obj)
    {
        _compiledExpression ??= SpecExpression.Compile();
        return _compiledExpression(obj);
    }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }
}