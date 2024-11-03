using System.Linq.Expressions;

namespace EntityArchitect.Entities.Repository
{
    public abstract class Specification<T> : ISpecification<T>
    {
        public Specification(Expression<Func<T, bool>> specExpression)
        {
            SpecExpression = specExpression;
        }
        public Expression<Func<T, bool>> SpecExpression { get; }

        private Func<T, bool>? _compiledExpression;

        public List<Expression<Func<T, object>>> Includes { get; } = new();

        public List<string> IncludeStrings { get; } = new();

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        public bool IsSatisfiedBy(T obj)
        {
            _compiledExpression ??= SpecExpression.Compile();
            return _compiledExpression(obj);
        }
    }
}