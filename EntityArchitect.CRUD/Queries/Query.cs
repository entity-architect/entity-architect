using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD.Queries;

public class Query<TEntity> where TEntity : Entity
{
    public string Sql { get; private set; }
    public bool UseSqlFile { get; private set; }
    public string SplitOn { get; private set; }
    protected Query(string sql)
    {
        Sql = sql;
        UseSqlFile = false;
        SplitOn = "";
    }    
    protected Query(string sql, bool useSqlFile)
    {
        Sql = sql;
        UseSqlFile = useSqlFile;
        SplitOn = "";
    }
    protected Query(string sql, bool useSqlFile = false, string splitOn = "")
    {
        Sql = sql;
        UseSqlFile = useSqlFile;
        SplitOn = splitOn;
    }
}