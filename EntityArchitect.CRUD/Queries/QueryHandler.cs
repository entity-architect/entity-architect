using System.Data;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Results.Abstracts;
using Npgsql;

namespace EntityArchitect.CRUD.Queries;

internal partial class QueryHandler<TParam, TEntity>
    where TParam : class
    where TEntity : Entity
{
    internal async Task<Result> HandleAsync(Query<TEntity> query, TParam param, string connectionString, CancellationToken cancellationToken = default)
    {
        using (IDbConnection dbConnection = new NpgsqlConnection(connectionString))
        {
            var sql = "";
            if (query.UseSqlFile)
            {
                sql = await File.ReadAllTextAsync(query.Sql, cancellationToken);
            }
            else
            {
                sql = RemoveTypes(query.Sql);
            }


            foreach (var props in param.GetType().GetProperties())
            {
                if (props.PropertyType != typeof(string)) continue;
                
                var sqlParameterPosition = props.GetCustomAttribute<SqlParameterPositionTypeAttribute>()?.Position;
                switch (sqlParameterPosition)
                {
                    case SqlParameterPosition.StartsWith:
                        props.SetValue(param, "%" + (props.GetValue(param) ?? ""));
                        break;
                    case SqlParameterPosition.EndsWith:
                        props.SetValue(param, (props.GetValue(param) ?? "") + "%");
                        break;
                    case SqlParameterPosition.Contains:
                        props.SetValue(param, "%" + (props.GetValue(param) ?? "") + "%");
                        break;
                    case SqlParameterPosition.Exact:
                        break;
                    case null:
                        break;
                }
            }
            
            dbConnection.Open();
            try
            {
                var type = new List<Type> {typeof(int), typeof(int), typeof(int), };
                var mapFunc = CreateMapFunction(type.ToArray());

                // Wykonujemy Query z dynamicznym splitOn
                var result = dbConnection.Query(sql, mapFunc, param, splitOn: query.SplitOn);
                return Result.Success(result);
            }
            catch (Exception e)
            {
                return Result.Failure(new Error(HttpStatusCode.InternalServerError, e.Message));
            }
        }
    }
    
    public static IEnumerable<object> QueryWithDynamicSplit(IDbConnection connection, string sql, Type[] types, string splitOn, object parameters = null)
    {
        var typeArray = types;
        var mapFunc = CreateMapFunction(typeArray);

        var result= connection.GetType().GetMethod("QueryAsync")?.MakeGenericMethod(typeArray).Invoke(connection, new[] {sql, mapFunc, parameters, splitOn = splitOn});
        

        return result as IEnumerable<object>;
    }
    
    private static Func<object[], object> CreateMapFunction(Type[] types)
    {
        return objects =>
        {
            var combinedResult = new List<object>();

            for (int i = 0; i < types.Length; i++)
            {
                combinedResult.Add(Convert.ChangeType(objects[i], types[i]));
            }

            return combinedResult;
        };
    }
    
    private static string RemoveTypes(string text)
    {
        var pattern = @"(@\w+):\w+(:\w+)?";
        return Regex.Replace(text, pattern, "$1");
    }
}