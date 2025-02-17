using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EntityArchitect.CRUD.Attributes.QueryResponseTypeAttributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.TypeBuilders;
using Npgsql;

namespace EntityArchitect.CRUD.Queries;

internal class QueryHandler<TParam, TEntity>
    where TParam : class
    where TEntity : Entity
{
    protected Query<TEntity> Query { get; set; }
    
    internal async Task<Result> HandleAsync(Query<TEntity> query, TParam param, string connectionString, Assembly assembly,
        CancellationToken cancellationToken = default)
    {
        Query = query;
        using IDbConnection dbConnection = new NpgsqlConnection(connectionString);
        string sql;
        if (query.UseSqlFile)
            sql = await File.ReadAllTextAsync(query.Sql, cancellationToken);
        else
            sql = SqlParser.RemoveTypes(query.Sql);

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
    
        sql = sql.Replace("\n", " ");
        var parametersFields = SqlParser.ParseSql(sql, assembly);
        dbConnection.Open();
        try
        {
            var result = QueryWithDynamicSplit(dbConnection, sql, parametersFields, param, query.GetType().Name, query);
            return Result.Success(result);
        }
        catch (Exception e)
        {
            return Result.Failure(new Error(HttpStatusCode.InternalServerError, e.Message));
        }
    }
   
    private static object QueryWithDynamicSplit(IDbConnection connection, string sql,
        List<SqlParser.Field> parameterFields, object param, string queryName, Query<TEntity> query)
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
        methods = methods.Where(m => m.Name == "Query").ToArray();
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

            Type resultTypeFinal = resultType;
            if (!query.Single)
                resultTypeFinal = typeof(List<>).MakeGenericType(resultType);
            
            var result = Activator.CreateInstance(resultTypeFinal)!;

            if (query.Single && grouped.Count != 0)
                grouped = grouped.Take(1).ToList();
            
            //TODO poinfomuj mnie jakoś że brakuje limit 1
            
            foreach (var groupedItem in grouped)
            {
                var convertedTypes = groupedItem.Select(c => MergeResult.ConvertType(resultType, c));
                var merged = MergeResult.MergeAllObjects(convertedTypes);
                
                if(query.Single)
                    return merged;
                
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
        if (types == null || types.Count <= 1)
            return types;

        var reorderedTypes = new List<Type>();
        var remainingTypes = new HashSet<Type>(types);

        Type baseType = types.FirstOrDefault(t => !types.Any(other => TypeHasPropertyOfType(other, t)));
        if (baseType == null)
            return types;

        reorderedTypes.Add(baseType);
        remainingTypes.Remove(baseType);

        while (remainingTypes.Count > 0)
        {
            Type nextType = remainingTypes.FirstOrDefault(t => reorderedTypes.Any(parent => TypeHasPropertyOfType(parent, t)));
            if (nextType == null)
            {
                reorderedTypes.AddRange(remainingTypes);
                break;
            }

            reorderedTypes.Add(nextType);
            remainingTypes.Remove(nextType);
        }

        return reorderedTypes;
    }

    private static bool TypeHasPropertyOfType(Type typeToCheck, Type requiredPropertyType)
    {
        return typeToCheck.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(prop => prop.PropertyType == requiredPropertyType);
    }


    
    private static object GetPropertyValue(object obj)
    {
        var props =  obj.GetType().GetProperties();
        return props.First(c => c.CustomAttributes
                .Any(attributeData => attributeData.AttributeType == typeof(IsKeyAttribute)))
            .GetValue(obj, null)!;
    }

    private static Delegate CreateMapFunction(Type[] types)
    {
        var parameters = types.Select((t, i) => Expression.Parameter(t, $"arg{i}")).ToArray();
        var argumentsArray =
            Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object))));

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