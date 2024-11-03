using System.Reflection;
using System.Reflection.Emit;
using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD;

public class TypeBuilder
{
    private List<Type> _types = new();

    public Type BuildCreateRequestFromEntity(Type entityType, Type? parentType = null)
    {
        bool isList = false;
        if (entityType.IsGenericType &&
            entityType.GetGenericTypeDefinition() == typeof(List<>))
        {
            isList = true;
            entityType = entityType.GetGenericArguments()[0];
        }

        var typeName = entityType.FullName + "CreateRequest";
        if (_types.Any(c => c.FullName == typeName))
            return _types.First(c => c.FullName == typeName);

        var typeBuilder = GetTypeBuilder(typeName, typeof(EntityRequest));
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
                CreateProperty(typeBuilder, property.Name + "Id", typeof(Guid));
                continue;
            }


            if (property.PropertyType.IsGenericType &&
                typeof(Entity) == property.PropertyType.GetGenericArguments()[0].BaseType)
            {
                var builtType =
                    BuildCreateRequestFromEntity(
                        typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]));
                CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var builtType = BuildCreateRequestFromEntity(property.PropertyType, entityType);
                CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);
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

        var typeBuilder = GetTypeBuilder(typeName, typeof(EntityRequest));
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

            CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);



            if (property.PropertyType.IsGenericType &&
                typeof(Entity) == property.PropertyType.GetGenericArguments()[0].BaseType)
            {
                var builtType =
                    BuildUpdateRequestFromEntity(
                        typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]));
                CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var builtType = BuildUpdateRequestFromEntity(property.PropertyType, entityType);
                CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);

        _types.Add(resultType);
        return resultType;
    }

    public Type BuildResponseFromEntity(Type entityType, Type? parentType = null)
    {
        bool isList = false;
        if (entityType.IsGenericType &&
            entityType.GetGenericTypeDefinition() == typeof(List<>))
        {
            isList = true;
            entityType = entityType.GetGenericArguments()[0];
        }

        var typeName = entityType.FullName + "Response";
        if (_types.Any(c => c.FullName == typeName))
            return _types.First(c => c.FullName == typeName);

        var typeBuilder = GetTypeBuilder(typeName, typeof(EntityResponse));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);
        var properties = entityType.GetProperties().OrderByDescending(s => s.Name.StartsWith("Id")).ToList();
        foreach (var property in properties)
        {
            if (property.PropertyType == parentType ||
                (parentType is not null &&
                 typeof(List<>).MakeGenericType(parentType) == property.PropertyType))
                continue;

            if (property.Name is "CreatedAt" or "UpdatedAt")
                continue;

            if (property.PropertyType.IsGenericType &&
                typeof(Entity) == property.PropertyType.GetGenericArguments()[0].BaseType)
            {
                var builtType =
                    BuildResponseFromEntity(
                        typeof(List<>).MakeGenericType(property.PropertyType.GetGenericArguments()[0]));
                CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var builtType = BuildResponseFromEntity(property.PropertyType, entityType);
                CreateProperty(typeBuilder, property.Name, builtType);
                continue;
            }

            CreateProperty(typeBuilder, property.Name, property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);

        _types.Add(resultType);
        return resultType;
    }

    private static System.Reflection.Emit.TypeBuilder GetTypeBuilder(string typeName, Type? parentType = null)
    {
        var assemblyName = new AssemblyName(typeName);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        return parentType is not null
            ? moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class, parentType)
            : moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
    }

    private static void CreateProperty(System.Reflection.Emit.TypeBuilder typeBuilder, string propertyName,
        Type propertyType)
    {
        var fieldBuilder = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);

        var propertyBuilder =
            typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
        var getMethodBuilder = typeBuilder.DefineMethod($"get_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            propertyType, Type.EmptyTypes);

        var getIl = getMethodBuilder.GetILGenerator();
        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, fieldBuilder);
        getIl.Emit(OpCodes.Ret);

        var setMethodBuilder = typeBuilder.DefineMethod($"set_{propertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, new[] { propertyType });

        var setIl = setMethodBuilder.GetILGenerator();
        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, fieldBuilder);
        setIl.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getMethodBuilder);
        propertyBuilder.SetSetMethod(setMethodBuilder);
    }
}
