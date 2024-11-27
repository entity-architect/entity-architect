namespace EntityArchitect.CRUD.Queries;

public class Query<TEntity>()
{
    public ICollection<string> Sql { get; protected set; } = [];
    protected Query(ICollection<string> sql) : this()
    {
        Sql = sql;
    }
}