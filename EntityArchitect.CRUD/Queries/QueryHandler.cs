using System.Data;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Dapper;
using EntityArchitect.CRUD.Attributes.QueryResponseTypeAttributes;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.TypeBuilders;
using Npgsql;

namespace EntityArchitect.CRUD.Queries;

internal class QueryHandler<TParam>
    where TParam : class
{
    
    internal async Task<Result> HandleAsync(string sql, string queryName, TParam param, string connectionString, Assembly assembly, string fileUrl, bool isSingle = false,
        CancellationToken cancellationToken = default)
    {
        isSingle = sql.Replace("--", "").ToUpper().Contains("#SINGLE") || isSingle;
        using IDbConnection dbConnection = new NpgsqlConnection(connectionString);
        queryName = queryName.Replace("/", "");

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
    
        var parametersFields = SqlParser.ParseSql(sql, assembly, fileUrl);
        sql = SqlParser.CleanupSql(sql, parametersFields);
        sql = sql.Replace("\n", " ");
        dbConnection.Open();
        try
        {
            var result = QueryWithDynamicSplit(dbConnection, sql, parametersFields, param, queryName, isSingle);
            return Result.Success(result);
        }
        catch (Exception e)
        {
            return Result.Failure(new Error(HttpStatusCode.InternalServerError, e.Message));
        }
    }
   
    private static object QueryWithDynamicSplit(IDbConnection connection, string sql,
        List<SqlParser.Field> parameterFields, object param, string queryName, bool isSingle = false)
    {
        TypeBuilder typeBuilder = new();
        var typeArray = typeBuilder.BuildQueryTypes(parameterFields, queryName, out var splitOn);
        typeArray = ReorderTypes(typeArray.ToList()).ToArray();
        var resultType = typeBuilder.BuildQueryResultType(typeArray.First());
        

        var dapperExtensions = typeof(SqlMapper);
        Console.WriteLine("Executing SQL: \n\n" + sql + " \n\n");

        var methods = dapperExtensions.GetMethods();
        methods = methods.Where(m => m.Name == "Query").ToArray();
        MethodInfo genericMethod;
        var useGenericParams = typeArray.Length == 1;
        if (useGenericParams)
        {
            var method = methods.FirstOrDefault(m =>
                m is { Name: "Query", IsGenericMethod: true } && m.GetGenericArguments().Length == 1);
            genericMethod = method!.MakeGenericMethod(typeArray.First());
        }
        else
        {
            genericMethod = methods.First(m =>
                m is { Name: "Query", IsGenericMethod: true } && m.GetGenericArguments().Length == 1 && m.GetParameters().Length >= 3 && m.GetParameters()[2].ParameterType == typeof(Type[]))
                .MakeGenericMethod(typeArray.First());
        }
        
        using var transaction = connection.BeginTransaction();

        var cleanSql = SqlParser.CleanupSql(sql, parameterFields);
        try
        {
            object? task = null;
            if (useGenericParams)
            {
                task = genericMethod.Invoke(null,
                    new[] { connection, cleanSql, param, transaction, false, null, null });
            }
            else
            {
                var map = CreateMapFunction(typeArray[0]);
                task = genericMethod.Invoke(null,
                    new[] { connection, cleanSql, typeArray, map, param, transaction, false, splitOn, null, null });
            }

            transaction.Commit();

            var sqlResponse = task as IEnumerable<object>;
            if (sqlResponse == null) throw new Exception("No response from database");

            var grouped = sqlResponse.GroupBy(GetPropertyValue).ToList();

            var resultTypeFinal = resultType;
            if (!isSingle)
                resultTypeFinal = typeof(List<>).MakeGenericType(resultType);
            
            var result = Activator.CreateInstance(resultTypeFinal)!;

            if (isSingle && grouped.Count != 0)
                grouped = grouped.Take(1).ToList();
            
            if (isSingle && grouped.Count == 0)
                return Result.Failure(new Error(HttpStatusCode.NotFound, "Element not found."));
            
            foreach (var groupedItem in grouped)
            {
                var convertedTypes = groupedItem.Select(c => MergeResult.ConvertType(resultType, c));
                var merged = MergeResult.MergeAllObjects(convertedTypes);
                
                if(isSingle)
                    return merged;
                
                result.GetType().GetMethod("Add")?.Invoke(result, new[] { merged });
            }

            return result;
        }
        catch (Exception e)
        {
            return Result.Failure(new Error(HttpStatusCode.InternalServerError, e.Message));
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
    
    private static Delegate CreateMapFunction(Type type)
    {
        var param = Expression.Parameter(typeof(object[]), "args");

        var method = typeof(QueryHandlerHelper)
            .GetMethod(nameof(QueryHandlerHelper.BuildResponse), new[] { typeof(object[]) })
            ?.MakeGenericMethod(type);

        if (method == null)
            throw new InvalidOperationException("Method BuildResponse not found.");

        var call = Expression.Call(method, param);
        var lambda = Expression.Lambda(call, param);

        return lambda.Compile();
    }
}