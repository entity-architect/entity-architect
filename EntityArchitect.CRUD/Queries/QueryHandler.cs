using System.Data;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Results.Abstracts;
using Npgsql;

namespace EntityArchitect.CRUD.Queries;

internal class QueryHandler<TParam, TEntity>
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
                //todo:
                //query have form name:type:table
                //or name:(name:type, name:type, name:type) () as type
                var groups = ParseSqlResponseProperties(sql);
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
                var type = new List<Type> {};

                var result = QueryWithDynamicSplit(dbConnection, sql, type.ToArray(), query.SplitOn, param);
                return Result.Success(result);
            }
            catch (Exception e)
            {
                return Result.Failure(new Error(HttpStatusCode.InternalServerError, e.Message));
            }
        }
    }

    private static IEnumerable<object> QueryWithDynamicSplit(IDbConnection connection, string sql, Type[] types, string splitOn, object parameters)
    {
        var typeArray = types;
        var mapFunc = CreateMapFunction(typeArray);

        var result= connection.GetType().GetMethod("QueryAsync")?.MakeGenericMethod(typeArray).Invoke(connection, new[] {sql, mapFunc, parameters, splitOn});
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
    
    public static List<(Type type, string name, string tableName)> ParseSqlResponseProperties(string input)
    {
        const string pattern = @"(?:\w+\.)?(\w+)(?: as (\w+))?:(\w+):(\w+)";
        var matches = Regex.Matches(input, pattern);
        var resultList = new List<(Type type, string name, string tableName)>();

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var name = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value;
                var typeString = match.Groups[3].Value;
                var tableName = match.Groups[4].Value;

                var type = TypeBuilder.ParseType(typeString);
                resultList.Add((type, name, tableName));
            }
        }

        return resultList;
    }
}