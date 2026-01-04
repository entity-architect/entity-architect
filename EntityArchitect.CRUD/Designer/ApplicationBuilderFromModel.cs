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
            
            // CannotCreate / CannotPost
            if (entity.CannotCreate || entity.CannotPost)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(CannotCreateAttribute).GetConstructor(Type.EmptyTypes)!,
                    Array.Empty<object>()));
            
            // CannotUpdate / CannotPut
            if (entity.CannotUpdate || entity.CannotPut)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(CannotUpdateAttribute).GetConstructor(Type.EmptyTypes)!,
                    Array.Empty<object>()));
            
            // CannotDelete
            if (entity.CannotDelete)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(CannotDeleteAttribute).GetConstructor(Type.EmptyTypes)!,
                    Array.Empty<object>()));
            
            // CannotGetById
            if (entity.CannotGetById)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(CannotGetByIdAttribute).GetConstructor(Type.EmptyTypes)!,
                    Array.Empty<object>()));
            
            // HasLightList
            if (entity.HasLightList)
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(HasLightListAttribute).GetConstructor(Type.EmptyTypes)!,
                    Array.Empty<object>()));
            
            // GetListPaginated
            if (entity.GetListPaginated)
            {
                var itemCount = entity.PaginatedItemCount ?? 10;
                entityAttributes.Add(new CustomAttributeBuilder(
                    typeof(GetListPaginatedAttribute).GetConstructor(new[] { typeof(int) })!,
                    new object[] { itemCount }));
            }
            
            var tb = TypeBuilderExtension.GetTypeBuilder(entity.Name, typeof(Entity), entityAttributes);
            
            foreach (var property in entity.Properties)
            {
                Console.WriteLine($"  Adding property: {property.Name} of type {property.Type}");
                
                var type = GetType(property.Type);
                
                List<CustomAttributeBuilder> attributes = new();

                // IgnorePostRequest
                if (property.IgnorePostRequest)
                    attributes.Add(new CustomAttributeBuilder(
                        typeof(IgnorePostRequest).GetConstructor(Type.EmptyTypes)!,
                        Array.Empty<object>()));
                
                // IgnorePutRequest
                if (property.IgnorePutRequest)
                    attributes.Add(new CustomAttributeBuilder(
                        typeof(IgnorePutRequest).GetConstructor(Type.EmptyTypes)!,
                        Array.Empty<object>()));
                
                // IncludeInGet
                if (property.IncludeInGet)
                {
                    var deep = property.IncludingDeep ?? 0;
                    attributes.Add(new CustomAttributeBuilder(
                        typeof(IncludeInGetAttribute).GetConstructor(new[] { typeof(int) })!,
                        new object[] { deep }));
                }
                
                // LightListProperty
                if (property.LightListProperty)
                    attributes.Add(new CustomAttributeBuilder(
                        typeof(LightListPropertyAttribute).GetConstructor(Type.EmptyTypes)!,
                        Array.Empty<object>()));

                if (property.Relation is not null)
                {
                    var checkIfExists = property.Relation.CheckIfExists;
                    var targetEntity = property.Relation.TargetEntity;
                    var targetPropertyName = property.Relation.TargetPropertyName;
                    
                    if (property.Relation.Type == RelationType.OneToOne)
                    {
                        // Use non-generic OneToOneAttribute(propertyName, entityName, checkIfExists)
                        attributes.Add(new CustomAttributeBuilder(
                            typeof(OneToOneAttribute).GetConstructor(new[] { typeof(string), typeof(string), typeof(bool) })!,
                            new object[] { targetPropertyName, targetEntity, checkIfExists }));
                    }
                    else if (property.Relation.Type == RelationType.OneToMany)
                    {
                        // Use non-generic OneToManyAttribute(propertyName, entityName, checkIfExists)
                        attributes.Add(new CustomAttributeBuilder(
                            typeof(OneToManyAttribute).GetConstructor(new[] { typeof(string), typeof(string), typeof(bool) })!,
                            new object[] { targetPropertyName, targetEntity, checkIfExists }));
                    }
                    else if (property.Relation.Type == RelationType.ManyToOne)
                    {
                        // Use non-generic ManyToOneAttribute(propertyName, entityName)
                        attributes.Add(new CustomAttributeBuilder(  
                            typeof(ManyToOneAttribute).GetConstructor(new[] { typeof(string), typeof(string) })!,
                            new object[] { targetPropertyName, targetEntity }));
                        
                        // For ManyToOne we need ICollection<TargetEntity> but type is not available yet
                        // We'll use object type for now - the actual type resolution happens at runtime
                        TypeBuilderExtension.CreateProperty(tb, property.Name, typeof(object), attributes);
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