using System;
using System.Linq;
using System.Text.RegularExpressions;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Enumerations;

namespace EntityArchitect.CRUD.Helpers;

public static partial class CrudSqlBuilder
{
    public static string BuildPostSql<TEntity>(TEntity entity, string entityName) where TEntity : Entity
    {
        var properties = typeof(TEntity).GetProperties();
        var sql = $"INSERT INTO \"{ToSnakeCase(entityName)}\" (";
        foreach (var property in properties)
        {
            if(property.PropertyType == typeof(EntityArchitect.CRUD.Files.File))
                continue;
            
            if (property.CustomAttributes
                .Select(c => c.AttributeType.Name)
                .Any(c => c.Contains("RelationManyToOneAttribute")))
                continue;

            if (property.CustomAttributes.Any(c =>
                    c.AttributeType.Name.Contains("RelationOneToManyAttribute")))
            {
                sql += $"{ToSnakeCase(property.Name)}_id, ";
                continue;
            }

            sql += $"{ToSnakeCase(property.Name)}, ";
        }

        sql = sql.Remove(sql.Length - 2) + ") VALUES (";
        var subQueries = new List<string>();
        foreach (var property in properties)
        {
            if(property.PropertyType == typeof(EntityArchitect.CRUD.Files.File))
                continue;

            if (property.CustomAttributes
                .Select(c => c.AttributeType.Name)
                .Any(c => c.Contains("RelationManyToOneAttribute")))
            {
                var values = property.GetValue(entity);
                
                if (values is null || property.PropertyType == typeof(TEntity))
                    continue;
                
                foreach (var value in values as IEnumerable<object>)
                {
                    var mi = typeof(CrudSqlBuilder)
                        .GetMethod(nameof(CrudSqlBuilder.BuildPostSql))
                        ?.MakeGenericMethod(value.GetType());
                    
                    var query = mi?.Invoke(null, new object[] {value, value.GetType().Name});
                    subQueries.Add(query as string);
                }
            }

            if (property.PropertyType == typeof(int))
                sql += property.GetValue(entity) + ", ";
            else if (property.CustomAttributes.Any(c => c.AttributeType.Name.Contains("RelationOneToManyAttribute")))
                sql += "'" + (property.GetValue(entity) as Entity).Id.Value + "', ";
            else if (property.CustomAttributes.Any(c => c.AttributeType.Name.Contains("RelationManyToOneAttribute")))
                sql+="";
            else if (property.PropertyType == typeof(DateTime))
                sql += "'" + ((DateTime)property.GetValue(entity)!).ToString("yyyy-MM-dd HH:mm:ss") + "', ";
            else if (property.PropertyType == typeof(bool))
                sql += (bool)property.GetValue(entity)! ? "true, " : "false, ";
            else if (property.Name == nameof(Entity.Id))
                sql += "'" + entity.Id.Value + "', ";
            else if (property.PropertyType.BaseType == typeof(Enumeration))
                sql += ((Enumeration)property.GetValue(entity)).Id + ", ";
            else
                sql += "'" + property.GetValue(entity) + "', ";
        }

        sql = sql.Remove(sql.Length - 2);
        sql += ");";
        
        foreach (var subQuery in subQueries)
        {
            sql += "\n" +  subQuery;
        }
        return sql;
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        var result = SnakeCaseRegex().Replace(input, "_$1").ToLower();

        return result;
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex SnakeCaseRegex();
}