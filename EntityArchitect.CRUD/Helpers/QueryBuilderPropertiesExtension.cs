using System.Reflection;
using System.Text.RegularExpressions;
using Docker.DotNet.Models;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Helpers;

public static class QueryBuilderPropertiesExtension
{
    public static void QueryBuilderPropertiesBuild(this IApplicationBuilder app)
    {
        var assembly = Assembly.GetEntryAssembly()!;
        var entities = assembly.GetTypes().Where(c => c.BaseType == typeof(Entity)).ToList();
        var result = "";
        
        foreach (var entity in entities)
        {
            string content = "SELECT ";
            var properties = entity.GetProperties();
            var entityShortcut = entity.Name.ToLower()[0];
            content += $"\n {entityShortcut}.id AS Id:GUID:Key,";
            foreach (var property in properties)
            {
                if(property.PropertyType == typeof(EntityArchitect.CRUD.Files.EntityFile))
                    continue;
                
                if(property.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationPasswordAttribute)))
                    continue;

                if(property.CustomAttributes.Any(c => c.AttributeType == typeof(ManyToOneAttribute<>)))
                    continue;
                
                if(property.CustomAttributes.Any(c => c.AttributeType == typeof(OneToManyAttribute<>)))
                    continue;
                
                if(property.CustomAttributes.Any(c => c.AttributeType == typeof(OneToOneAttribute<>)))
                    continue;

                if (property.Name == "Id")
                    continue;
                
                var propertyType = property.PropertyType;
                var typeName = propertyType switch
                {
                    not null when propertyType == typeof(string) => "STRING",
                    not null when propertyType == typeof(int) => "INT",
                    not null when propertyType == typeof(double) => "DOUBLE",
                    not null when propertyType == typeof(decimal) => "DECIMAL",
                    not null when propertyType == typeof(DateTime) => "DATETIME",
                    not null when propertyType == typeof(bool) => "BOOLEAN",
                    
                    _ => "ERR"
                };
                if(typeName == "ERR")
                    continue;
                
                var snakeCaseName = ToSnakeCase(property.Name);
                content += $"\n {entityShortcut}.{snakeCaseName} AS {property.Name}:{typeName},";
            }
            
            content = content.Remove(content.Length - 1);
            result += "-------------------------------\n\n";
            result += content + $"\nFROM {ToSnakeCase(entity.Name)} {entityShortcut}\n\n";
        }
        File.WriteAllText("query.txt", result);

    }
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var startUnderscores = Regex.Match(input, @"^_+");
        return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
    }
    
}