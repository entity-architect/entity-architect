using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using EntityArchitect.CRUD.Attributes.QueryResponseTypeAttributes;
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
        using IDbConnection dbConnection = new NpgsqlConnection(connectionString);
        string sql;
        if (query.UseSqlFile)
        {
            sql = await File.ReadAllTextAsync(query.Sql, cancellationToken);
        }
        else
        {
            sql = SqlParser.RemoveTypes(query.Sql);
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
    
    private static object QueryWithDynamicSplit(IDbConnection connection, string sql,
        List<SqlParser.Field> parameterFields, object param, string queryName)
    {
        TypeBuilder typeBuilder = new();
        var typeArray = typeBuilder.BuildQueryTypes(parameterFields, queryName, out var splitOn);
        typeArray = ReorderTypes(typeArray.ToList()).ToArray();
        
        var resultType = typeBuilder.BuildQueryResultType(typeArray.First());
        var typList = typeArray.ToList();
        typList.Add(typeArray.First());
        var map = CreateMapFunction(typeArray);
        typeArray = typList.ToArray();

        var dapperExtensions = typeof(SqlMapper);

        var methods = dapperExtensions.GetMethods();
        var method = methods.FirstOrDefault(m =>
            m is { Name: "Query", IsGenericMethod: true } && m.GetGenericArguments().Length == typeArray.Length);
        var genericMethod = method!.MakeGenericMethod(typeArray);
        using var transaction = connection.BeginTransaction();

        var cleanSql = SqlParser.CleanupSql(sql);
        try
        {
            var task = genericMethod.Invoke(null,
                new[] { connection, cleanSql, map, param, transaction, false, splitOn, null, null });
            transaction.Commit();
            var sqlResponse = task as IEnumerable<object>;
            if (sqlResponse == null) throw new Exception("No response from database");

            var grouped = sqlResponse.GroupBy(GetPropertyValue).ToList();
            var result = Activator.CreateInstance(typeof(List<>).MakeGenericType(resultType))!;

            foreach (var groupedItem in grouped)
            {
                var convertedTypes = groupedItem.Select(c => MergeResult.ConvertType(resultType, c));
                var merged = MergeResult.MergeAllObjects(convertedTypes);
                result.GetType().GetMethod("Add")?.Invoke(result, new[] { merged });
            }
            
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static List<Type> ReorderTypes(List<Type> types)
    {
        for (var i = 0; i < types.Count - 1; i++)
        {
            var currentType = types[i];
            var nextType = types[i + 1];

            if (!TypeHasPropertyOfType(currentType, nextType))
            {
                types[i] = nextType;
                types[i + 1] = currentType;
                
                if (i > 0)
                {
                    i -= 2;
                }
            }
            else
            {                
                i++;
            }
        }

        return types;
    }

    private static bool TypeHasPropertyOfType(Type typeToCheck, Type requiredPropertyType)
    {
        return typeToCheck.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(prop => prop.PropertyType == requiredPropertyType);
    }
    

    static object GetPropertyValue(object obj)
    {
        return obj.GetType().GetProperties()
            .First(c => c.CustomAttributes.Any(attributeData => attributeData.AttributeType == typeof(IsKeyAttribute)))
            .GetValue(obj, null)!;
    }

    private static Delegate CreateMapFunction(Type[] types)
    {
        var parameters = types.Select((t, i) => Expression.Parameter(t, $"arg{i}")).ToArray();
        var argumentsArray = Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object))));

        var method = typeof(QueryHandlerHelper).GetMethod(nameof(QueryHandlerHelper.BuildResponse));
        method = method!.MakeGenericMethod(types[0]);
        var callMethod = Expression.Call(
            method,
            argumentsArray
        );

        var lambda = Expression.Lambda(callMethod, parameters);
        return lambda.Compile();
    }
}