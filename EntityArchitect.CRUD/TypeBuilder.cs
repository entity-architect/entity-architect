using System.Reflection;
using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD;

public class TypeBuilder(int maxIncludingDeep = 1)
{
    private readonly List<Type> _types = [];
    public Type BuildCreateRequestFromEntity(Type entityType, Type? parentType = null)
    {
        var isList = false;
        if (entityType.IsGenericType &&
            entityType.GetGenericTypeDefinition() == typeof(List<>))
        {
            isList = true;
            entityType = entityType.GetGenericArguments()[0];
        }

        var typeName = entityType.FullName + "CreateRequest";
        if (_types.Any(c => c.FullName == typeName))
            return _types.First(c => c.FullName == typeName);

        var typeBuilder = TypeBuilderExtension.GetTypeBuilder(typeName, typeof(EntityRequest));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);
        var properties = entityType.GetProperties().OrderByDescending(s => s.Name.StartsWith("Id")).ToList();
        foreach (var property in properties)
        {
            if (property.PropertyType == parentType ||
                (parentType is not null &&
                 typeof(List<>).MakeGenericType(parentType) == property.PropertyType))
                continue;

            if (property.Name is "Id" or "CreatedAt" or "UpdatedAt" ||
                property.CustomAttributes.Select(c => c.AttributeType).Contains(typeof(IgnorePostRequest)))
                continue;
            var attributeType = typeof(RelationOneToManyAttribute<>)
                .MakeGenericType(property.PropertyType);

            if (property.CustomAttributes.Select(c => c.AttributeType).Contains(attributeType))
            {
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name + "Id", typeof(Guid));
                continue;
            }


            if (property.PropertyType.IsGenericType &&
                typeof(Entity) == property.PropertyType.GetGenericArguments()[0].BaseType)
            {
                var builtType =
                    BuildCreateRequestFromEntity(
                        typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]));
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var builtType = BuildCreateRequestFromEntity(property.PropertyType, entityType);
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);

        _types.Add(resultType);
        return resultType;
    }
    public Type BuildUpdateRequestFromEntity(Type entityType, Type? parentType = null)
    {
        bool isList = false;
        if (entityType.IsGenericType &&
            entityType.GetGenericTypeDefinition() == typeof(List<>))
        {
            isList = true;
            entityType = entityType.GetGenericArguments()[0];
        }

        var typeName = entityType.FullName + "UpdateRequest";
        if (_types.Any(c => c.FullName == typeName))
            return _types.First(c => c.FullName == typeName);

        var typeBuilder = TypeBuilderExtension.GetTypeBuilder(typeName, typeof(EntityRequest));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);
        var properties = entityType.GetProperties().OrderByDescending(s => s.Name.StartsWith("Id")).ToList();
        foreach (var property in properties)
        {
            if (property.PropertyType == parentType ||
                (parentType is not null &&
                 typeof(List<>).MakeGenericType(parentType) == property.PropertyType))
                continue;

            if (property.Name is "CreatedAt" or "UpdatedAt" ||
                property.CustomAttributes.Select(c => c.AttributeType).Contains(typeof(IgnorePutRequest)))
                continue;

            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);

            if (property.PropertyType.IsGenericType &&
                typeof(Entity) == property.PropertyType.GetGenericArguments()[0].BaseType)
            {
                var builtType =
                    BuildUpdateRequestFromEntity(
                        typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]));
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var builtType = BuildUpdateRequestFromEntity(property.PropertyType, entityType);
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);

        _types.Add(resultType);
        return resultType;
    }
    public Type BuildResponseFromEntity(Type entityType, List<Type>? parentType = null)
    {
        var isList = false;
        if (entityType.IsGenericType &&
            entityType.GetGenericTypeDefinition() == typeof(List<>))
        {
            isList = true;
            entityType = entityType.GetGenericArguments()[0];
        }

        var nameParentTypes = "";
        foreach (var item in (parentType?.Select(c => c.Name) ?? Array.Empty<string>()).ToList())
        {
            nameParentTypes += item;
        }
        
        var typeName = entityType.FullName + nameParentTypes + "Response";
        if (_types.Any(c => c.FullName == typeName))
            return _types.First(c => c.FullName == typeName);

        var typeBuilder = TypeBuilderExtension.GetTypeBuilder(typeName, typeof(EntityResponse));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);
        
        var properties = entityType.GetProperties().OrderByDescending(s => s.Name.StartsWith("Id")).ToList();
        foreach (var property in properties)
        {
            if (parentType is not null &&
                parentType.Count > 0 &&
                property.PropertyType == parentType.Last() ||
                (parentType is not null &&
                 typeof(List<>).MakeGenericType(parentType.Last()) == property.PropertyType))
            {
                if (parentType.Count > maxIncludingDeep)
                    continue;
                
                parentType.Add(entityType);
                var builtType = BuildResponseFromEntity(property.PropertyType, parentType);
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            if (property.Name is nameof(Entity.CreatedAt) or nameof(Entity.UpdatedAt))
                continue;

            if (property.PropertyType.IsGenericType &&
                typeof(Entity) == property.PropertyType.GetGenericArguments()[0].BaseType)
            {
                if (parentType is not null && parentType.Count > maxIncludingDeep)
                    continue;
                
                parentType ??= [];
                parentType.Add(entityType);
                var builtType =
                    BuildResponseFromEntity(
                        typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]), parentType);
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                if (parentType is not null && parentType.Count > maxIncludingDeep)
                    continue;
                
                parentType ??= [];
                parentType.Add(entityType);
                var builtType = BuildResponseFromEntity(property.PropertyType, parentType);
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }
            
            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        _types.Add(resultType);
        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);
        
        return resultType;
    }
}
