using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Enumerations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EntityArchitect.CRUD.Helpers;

public static partial class CrudSqlBuilder
{
    public static string BuildPostSql<TEntity>(TEntity entity, string entityName) where TEntity : Entity
    {
        var properties = typeof(TEntity).GetProperties();
        var sql = $"INSERT INTO \"{ToSnakeCase(entityName)}\" (";
        foreach (var property in properties)
        {
            if(property.PropertyType == typeof(EntityArchitect.CRUD.Files.EntityFile))
                continue;
            
            if (property.CustomAttributes
                .Select(c => c.AttributeType.Name)
                .Any(c => c.Contains("ManyToOneAttribute")))
                continue;

            if (property.CustomAttributes.Any(c =>
                    c.AttributeType.Name.Contains("OneToManyAttribute")))
            {
                sql += $"{ToSnakeCase(property.Name)}_id, ";
                continue;
            }
            
            if (property.CustomAttributes.Any(c =>
                    c.AttributeType.Name.Contains("OneToOneAttribute")))
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
            if(property.PropertyType == typeof(EntityArchitect.CRUD.Files.EntityFile))
                continue;

            if (property.CustomAttributes
                .Select(c => c.AttributeType.Name)
                .Any(c => c.Contains("ManyToOneAttribute")))
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

            if (property.CustomAttributes.Any(c => c.AttributeType.Name.Contains("OneToManyAttribute")))
            {
                var val = property.GetValue(entity);
                sql += val is not null ? "'" + (val as Entity)?.Id.Value + "', " : "null, ";
            }
            else if (property.CustomAttributes.Any(c => c.AttributeType.Name.Contains("OneToOneAttribute")))
            {
                var val = property.GetValue(entity);
                sql += val is not null ? "'" + (val as Entity)?.Id.Value + "', " : "null, ";
            }
            else if (property.CustomAttributes.Any(c => c.AttributeType.Name.Contains("ManyToOneAttribute")))
                sql += "";
            else
            {
                var val = property.GetValue(entity);
                var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                if (val is null)
                    sql += "null, ";
                else if (property.Name == nameof(Entity.Id))
                    sql += "'" + entity.Id.Value + "', ";
                else if (underlyingType == typeof(int))
                    sql += val + ", ";
                else if (underlyingType == typeof(DateTime))
                    sql += "'" + ((DateTime)val).ToString("yyyy-MM-dd HH:mm:ss") + "', ";
                else if (underlyingType == typeof(bool))
                    sql += (bool)val ? "true, " : "false, ";
                else if (underlyingType.BaseType == typeof(Enumeration))
                    sql += ((Enumeration)val).Id + ", ";
                else
                    sql += "'" + val + "', ";
            }
        }

        sql = sql.Remove(sql.Length - 2);
        sql += ");";
        
        foreach (var subQuery in subQueries)
        {
            sql += "\n" +  subQuery;
        }
        return sql;
    }

    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        var result = SnakeCaseRegex().Replace(input, "_$1").ToLower();

        return result;
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex SnakeCaseRegex();
}