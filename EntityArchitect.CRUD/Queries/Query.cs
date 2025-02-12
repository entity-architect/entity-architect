using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Queries;

public class Query<TEntity> where TEntity : Entity
{
    protected Query(string sql)
    {
        Sql = sql;
        UseSqlFile = false;
        Single = false;
    }

    protected Query(string sql, bool useSqlFile)
    {
        Sql = sql;
        UseSqlFile = useSqlFile;
        Single = false;
    }

    protected Query(string sql, bool useSqlFile, bool single)
    {
        Sql = sql;
        UseSqlFile = useSqlFile;
        Single = single;
    }

    public string Sql { get; private set; }
    public bool UseSqlFile { get; private set; }
    public bool Single { get; private set; }
}