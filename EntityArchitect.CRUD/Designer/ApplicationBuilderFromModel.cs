using System.Reflection;
using System.Reflection.Emit;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Designer.ApplicationModels;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.TypeBuilders;
using Newtonsoft.Json;

namespace EntityArchitect.CRUD.Designer;

public static class ApplicationBuilderFromModel
{
    public static async Task BuildAsync(this IApplicationBuilder builder)
    {
        var lastMigrationFile = Directory.GetFiles("ApplicationMigrations").MaxBy(f => f);
        if (lastMigrationFile is null)
            throw new InvalidOperationException("No migration files found.");
        
        var json = await File.ReadAllTextAsync(lastMigrationFile);
        var model = JsonConvert.DeserializeObject<ApplicationModel>(json);
        
        if (model is null)
            throw new InvalidOperationException("Failed to deserialize the application model.");
        
        foreach (var entity in model.Entities)
        {
            Console.WriteLine($"Building entity: {entity.Name}");
            List<CustomAttributeBuilder> entityAttributes = new();
            if(entity.CannotDelete)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(CannotDeleteAttribute).GetConstructor(new[] { typeof(CannotDeleteAttribute) })!,
                    Array.Empty<object>()));
                
            if(entity.CannotPost)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(CannotDeleteAttribute).GetConstructor(new[] { typeof(CannotDeleteAttribute) })!,
                    Array.Empty<object>()));
                
            if(entity.CannotPut)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(CannotUpdateAttribute).GetConstructor(new[] { typeof(CannotUpdateAttribute) })!,
                    Array.Empty<object>()));
            var tb = TypeBuilderExtension.GetTypeBuilder(entity.Name, typeof(Entity));
            foreach (var property in entity.Properties)
            {
                Console.WriteLine($"  Adding property: {property.Name} of type {property.Type}");
                
                var type = GetType(property.Type);
                
                List<CustomAttributeBuilder> attributes = new();

                
                if(property.IgnorePostRequest)
                    attributes.Add(new CustomAttributeBuilder(
                        typeof(IgnorePostRequest).GetConstructor(new[] { typeof(IgnorePostRequest) })!,
                        Array.Empty<object>()));
                if(property.IgnorePutRequest)
                    attributes.Add(new CustomAttributeBuilder(
                        typeof(IgnorePutRequest).GetConstructor(new[] { typeof(IgnorePutRequest) })!,
                        Array.Empty<object>()));

                if (property.Relation is not null)
                {
                    if (property.Relation.Type == RelationType.OneToOne)
                    {
                        var attributeType = typeof(OneToOneAttribute<>).MakeGenericType(GetType(property.Relation.TargetEntity));
                        attributes.Add(new CustomAttributeBuilder(
                            attributeType.GetConstructor(new[] { typeof(string), typeof(bool) })!,
                            new object[] { property.Relation.TargetPropertyName, true }));
                    }
                    else if (property.Relation.Type == RelationType.OneToMany)
                    {
                        var attributeType = typeof(OneToManyAttribute<>).MakeGenericType(GetType(property.Relation.TargetEntity));
                        attributes.Add(new CustomAttributeBuilder(
                            attributeType.GetConstructor(new[] { typeof(string), typeof(bool) })!,
                            new object[] { property.Relation.TargetPropertyName, true }));
                    }
                    else if (property.Relation.Type == RelationType.ManyToOne)
                    {
                        
                        var attributeType = typeof(ManyToOneAttribute<>).MakeGenericType(GetType(property.Relation.TargetEntity));
                        attributes.Add(new CustomAttributeBuilder(  
                            attributeType.GetConstructor(new[] { typeof(string), typeof(bool) })!,
                            new object[] { property.Relation.TargetPropertyName, true }));
                        
                        var collectionType = typeof(ICollection<>).MakeGenericType(type);
                        TypeBuilderExtension.CreateProperty(tb, property.Name, collectionType, attributes);
                        continue;
                    }
                }
                
                TypeBuilderExtension.CreateProperty(tb, property.Name, type, attributes);
            }
        }
    }
    
    private static Type GetType(string typeName)
    {
        var t = typeName.ToLower() switch
        {
            "string" => typeof(string),
            "int" => typeof(int),
            "long" => typeof(long),
            "bool" => typeof(bool),
            "datetime" => typeof(DateTime),
            "double" => typeof(double),
            _ => typeof(object)
        };
        
        if(t == typeof(object))
            t = Assembly.GetEntryAssembly()!.GetType();
        
        return t;
    }
    
    public static async Task MigrateAsync(this Assembly assembly)
    {
        var folderPath = "ApplicationMigrations";
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
        
        var fileName = Path.Combine(folderPath, "Migration" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json");
        
        await File.WriteAllTextAsync(fileName, "{}");
    }
}