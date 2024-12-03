using System.Data;
using System.Linq.Expressions;
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
    internal async Task<Result> HandleAsync(Query<TEntity> query, TParam param, string connectionString,
        CancellationToken cancellationToken = default)
    {
        using (IDbConnection dbConnection = new NpgsqlConnection(connectionString))
        {
            string sql;
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

            var parametersFields = SqlParser.ParseSql(sql);
            dbConnection.Open();
            try
            {
                var result = QueryWithDynamicSplit(dbConnection, sql, parametersFields, param, query.GetType().Name);
                return Result.Success(result);
            }
            catch (Exception e)
            {
                return Result.Failure(new Error(HttpStatusCode.InternalServerError, e.Message));
            }
        }
    }

    private static List<object> QueryWithDynamicSplit(IDbConnection connection, string sql,
        List<SqlParser.Field> parameterFields, object param, string queryName)
    {
        TypeBuilder typeBuilder = new();
        var typeArray = typeBuilder.BuildQueryTypes(parameterFields, queryName, out var splitOn);

        typeArray = typeArray.Reverse().ToArray();
        var map = CreateMapFunction(typeArray);

        var fullTypeArray = typeArray.ToList();
        fullTypeArray.Add(typeArray[0]);
        typeArray = fullTypeArray.ToArray();
        var dapperExtensions = typeof(SqlMapper);

        var methods = dapperExtensions.GetMethods();
        var method = methods.FirstOrDefault(m =>
            m is { Name: "Query", IsGenericMethod: true } && m.GetGenericArguments().Length == typeArray.Length);
        var genericMethod = method!.MakeGenericMethod(typeArray);
        using var transaction = connection.BeginTransaction();

        var cleanSql = CleanSqlQuery(sql);
        try
        {
            var task = genericMethod.Invoke(null,
                new[] { connection, cleanSql, map, param, transaction, false, splitOn, null, null });
            transaction.Commit();
            var result = task as IEnumerable<object>;
            var x = result.ToList();
            return x;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }

    private static Delegate CreateMapFunction(Type[] types)
    {
        var parameters = types.Select((t, i) => Expression.Parameter(t, $"arg{i}")).ToArray();
        var argumentsArray = Expression.NewArrayInit(typeof(object), parameters);

        var method = typeof(QueryHandlerHelper).GetMethod(nameof(QueryHandlerHelper.BuildResponse));
        method = method!.MakeGenericMethod(types[0]);
        var callMethod = Expression.Call(
            method,
            argumentsArray
        );

        var lambda = Expression.Lambda(callMethod, parameters);
        return lambda.Compile();
    }


    static string RemoveTypes(string text)
    {
        const string pattern = @"(@\w+):\w+(:\w+)?";
        return Regex.Replace(text, pattern, "$1");
    }

    static string CleanSqlQuery(string query)
    {
        var step1 = Regex.Replace(query, @":\w+", "", RegexOptions.IgnoreCase);
        var step2 = Regex.Replace(step1, @"\w+:\((.*?)\)\[]?", "$1", RegexOptions.IgnoreCase);
        return step2.Trim();
    }
}