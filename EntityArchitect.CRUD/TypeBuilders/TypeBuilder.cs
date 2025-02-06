using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Attributes.QueryResponseTypeAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Enumerations;
using EntityArchitect.CRUD.Queries;
using LightListPropertyAttribute = EntityArchitect.CRUD.Attributes.CrudAttributes.LightListPropertyAttribute;

namespace EntityArchitect.CRUD.TypeBuilders;

public partial class TypeBuilder
{
    private List<Type> _types = [];

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

        if (_types.Any(c => c.IsGenericType && c.GetGenericArguments()[0].FullName == typeName))
            return _types.First(c => c.IsGenericType && c.GetGenericArguments()[0].FullName == typeName)
                .GetGenericArguments()[0];

        var customAttributeBuilder = new CustomAttributeBuilder(
            typeof(EntityRequestAttribute).GetConstructor(new[] { typeof(Type) })!, new object[] { entityType });
        var typeBuilder = TypeBuilderExtension.GetTypeBuilder(typeName, typeof(EntityRequest), customAttributeBuilder);
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);

        var properties = entityType.GetProperties().OrderByDescending(s => s.Name.StartsWith("Id")).ToList();
        foreach (var property in properties)
        {
            if (property.PropertyType == parentType ||
                (parentType is not null &&
                 typeof(List<>).MakeGenericType(parentType) == property.PropertyType))
                continue;

            if (property.PropertyType.BaseType == typeof(Enumeration))
            {
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, typeof(int));
                continue;
            }

            if (property.Name is "Id" or "CreatedAt" or "UpdatedAt" ||
                property.CustomAttributes.Select(c => c.AttributeType).Contains(typeof(IgnorePostRequest)))
                continue;

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var attributeType = typeof(RelationOneToManyAttribute<>)
                    .MakeGenericType(property.PropertyType);

                if (property.CustomAttributes.Select(c => c.AttributeType).Contains(attributeType))
                {
                    TypeBuilderExtension.CreateProperty(typeBuilder, property.Name + "Id", typeof(Guid));
                    continue;
                }
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

            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name,
                property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);

        _types.Add(resultType);
        _types = _types.OrderBy(c => c.FullName).ToList();
        return resultType;
    }

    public Type BuildUpdateRequestFromEntity(Type entityType, Type? parentType = null)
    {
        var isList = false;
        if (entityType.IsGenericType &&
            entityType.GetGenericTypeDefinition() == typeof(List<>))
        {
            isList = true;
            entityType = entityType.GetGenericArguments()[0];
        }
        
        

        var typeName = entityType.FullName + "UpdateRequest";
        if (_types.Any(c => c.FullName == typeName))
            return _types.First(c => c.FullName == typeName);

        if (_types.Any(c => c.IsGenericType && c.GetGenericArguments()[0].FullName == typeName))
            return _types.First(c => c.IsGenericType && c.GetGenericArguments()[0].FullName == typeName)
                .GetGenericArguments()[0];

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
            
            if (property.PropertyType.BaseType == typeof(Enumeration))
            {
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, typeof(int));
                continue;
            }

            if (property.Name is "CreatedAt" or "UpdatedAt" ||
                property.CustomAttributes.Select(c => c.AttributeType).Contains(typeof(IgnorePutRequest)))
                continue;

            if (property.PropertyType.BaseType == typeof(Entity))
            {
                var attributeType = typeof(RelationOneToManyAttribute<>)
                    .MakeGenericType(property.PropertyType);

                if (property.CustomAttributes.Select(c => c.AttributeType).Contains(attributeType))
                {
                    TypeBuilderExtension.CreateProperty(typeBuilder, property.Name + "Id", typeof(Guid));
                    continue;
                }
            }

            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name,
                property.Name == "Id" ? typeof(Guid) : property.PropertyType);

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

            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name,
                property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);

        _types.Add(resultType);
        _types = _types.OrderBy(c => c.FullName).ToList();
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
            nameParentTypes += item;

        var typeName = entityType.FullName + nameParentTypes + "Response";
        if (_types.Any(c => c.FullName == typeName))
            return _types.First(c => c.FullName == typeName);

        if (_types.Any(c => c.IsGenericType && c.GetGenericArguments()[0].FullName == typeName))
            return _types.First(c => c.IsGenericType && c.GetGenericArguments()[0].FullName == typeName)
                .GetGenericArguments()[0];

        var typeBuilder = TypeBuilderExtension.GetTypeBuilder(typeName, typeof(EntityResponse));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);

        var properties = entityType.GetProperties().OrderByDescending(s => s.Name.StartsWith("Id")).ToList();
        foreach (var property in properties)
        {
            if(property.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationPasswordAttribute)))
                continue;
            
            var maxIncludingDeep = (int)(property.CustomAttributes
                                             .FirstOrDefault(c => c.AttributeType == typeof(IncludeInGetAttribute))
                                             ?.ConstructorArguments[0].Value ??
                                         0);

            if ((parentType is not null &&
                 parentType.Count > 0 &&
                 property.PropertyType == parentType.Last()) ||
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

            TypeBuilderExtension.CreateProperty(typeBuilder, property.Name,
                property.Name == "Id" ? typeof(Guid) : property.PropertyType);
        }

        if (_types.Any(c => c.FullName == typeBuilder.FullName))
            return _types.First(c => c.FullName == typeBuilder.FullName);

        var resultType = typeBuilder.CreateType();
        _types.Add(resultType);
        _types = _types.OrderBy(c => c.FullName).ToList();

        if (isList)
            resultType = typeof(List<>).MakeGenericType(resultType);

        return resultType;
    }

    public Type BuildLightListProperty(Type entityType)
    {
        var typeName = entityType.FullName + "LightListResponse";
        var typeBuilder = TypeBuilderExtension.GetTypeBuilder(typeName, typeof(EntityResponse));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);

        var properties = entityType.GetProperties().OrderByDescending(s => s.Name.StartsWith("Id")).ToList();
        foreach (var property in properties)
        {
            if (property.PropertyType.BaseType != typeof(object) &&
                property.CustomAttributes.Any(c => c.AttributeType == typeof(LightListPropertyAttribute)))
                throw new Exception("Property in light list response must be a simple type");

            if (property.Name is nameof(Entity.Id))
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, typeof(Guid));

            if (property.CustomAttributes.Any(c => c.AttributeType == typeof(LightListPropertyAttribute)))
                TypeBuilderExtension.CreateProperty(typeBuilder, property.Name, property.PropertyType);
        }

        var resultType = typeBuilder.CreateType();
        _types.Add(resultType);
        _types = _types.OrderBy(c => c.FullName).ToList();

        return resultType;
    }

    public Type BuildQueryRequest(string sql, string requestName)
    {
        var properties = GetProperties(sql);
        var typeName = "Query" + requestName;
        var typeBuilder = TypeBuilderExtension.GetTypeBuilder(typeName, typeof(EntityRequest));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);

        foreach (var (type, name, sqlParameterPosition) in properties)
        {
            List<CustomAttributeBuilder> customAttributeBuilders = new();
            var attributeBuilder =
                new CustomAttributeBuilder(
                    typeof(SqlParameterPositionTypeAttribute).GetConstructor(new[] { typeof(SqlParameterPosition) })!,
                    new object[] { sqlParameterPosition });

            customAttributeBuilders.Add(attributeBuilder);

            TypeBuilderExtension.CreateProperty(typeBuilder, name, type, customAttributeBuilders);
        }

        var resultType = typeBuilder.CreateType();
        _types.Add(resultType);
        _types = _types.OrderBy(c => c.FullName).ToList();

        return resultType;
    }

    private static List<(Type type, string name, SqlParameterPosition sqlParameterPosition)> GetProperties(string sql)
    {
        var getProperties = new List<(Type type, string name, SqlParameterPosition sqlParameterPosition)>();
        var matches = GetPropertiesFromSqlRegex().Matches(sql);

        foreach (Match match in matches)
        {
            if (!match.Success) continue;
            var name = match.Groups[1].Value.Trim();
            var type = match.Groups[2].Value.Trim();

            var parsedType = ParseType(type);

            SqlParameterPosition sqlParameterPosition;
            if (int.TryParse(match.Groups[3].Value, out var sqlParameterPositionInt))
                sqlParameterPosition = (SqlParameterPosition)sqlParameterPositionInt;
            else
                sqlParameterPosition = SqlParameterPosition.Exact;


            getProperties.Add((parsedType, name, sqlParameterPosition));
        }

        return getProperties;
    }

    [GeneratedRegex(@"@(\w+):([A-Z]+)(?::(\d))?")]
    private static partial Regex GetPropertiesFromSqlRegex();

    private static Type ParseType(string typeString)
    {
        return typeString switch
        {
            "INT" => typeof(int),
            "STRING" => typeof(string),
            "DATETIME" => typeof(DateTime),
            "GUID" => typeof(Guid),
            "DECIMAL" => typeof(decimal),
            "FLOAT" => typeof(float),
            "DOUBLE" => typeof(double),
            "BOOL" => typeof(bool),
            "BOOLEAN" => typeof(bool),
            "BYTE" => typeof(byte),
            _ => typeof(string)
        };
    }

    public Type[] BuildQueryTypes(List<SqlParser.Field> parameterFields, string queryName, out string splitOn)
    {
        splitOn = "";
        var queryTypes = new List<Type>();

        var baseType = TypeBuilderExtension.GetTypeBuilder(queryName);
        baseType.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                          MethodAttributes.RTSpecialName);

        foreach (var field in parameterFields)
            if (field.Fields.Count == 0)
            {
                var propertyType = ParseType(field.Type);
                var attributesList = new List<CustomAttributeBuilder>();
                if (field.IsKey)
                {
                    var ctor = typeof(IsKeyAttribute).GetConstructors().First();
                    var customAttributeBuilder = new CustomAttributeBuilder(ctor!, new object[] { });
                    attributesList.Add(customAttributeBuilder);
                }

                TypeBuilderExtension.CreateProperty(baseType, field.Name, propertyType, attributesList);
            }
            else
            {
                var nestedType = BuildComplexType(field, queryName + field.Type, ref splitOn, ref queryTypes,
                    field.Fields.Any());

                var attributesList = new List<CustomAttributeBuilder>();
                if (field.IsArray)
                {
                    var ctor = typeof(IsArrayAttribute).GetConstructors().First();
                    var customAttributeBuilder = new CustomAttributeBuilder(ctor!, new object[] { });
                    attributesList.Add(customAttributeBuilder);
                }

                if (field.Fields.Any())
                {
                    var isNestedCtor = typeof(NestedTypeAttribute).GetConstructors().First();
                    var customAttributeIsNestedTypeBuilder =
                        new CustomAttributeBuilder(isNestedCtor!, new object[] { });
                    attributesList.Add(customAttributeIsNestedTypeBuilder);
                }

                TypeBuilderExtension.CreateProperty(baseType, field.Name, nestedType, attributesList);
            }

        var queryType = baseType.CreateType();
        _types.Add(queryType);
        _types = _types.OrderBy(c => c.FullName).ToList();

        if (!queryTypes.Contains(queryType)) queryTypes.Insert(0, queryType);

        if (splitOn.Length > 0)
            splitOn = splitOn[..^2];

        return queryTypes.ToArray();
    }


    private Type BuildComplexType(SqlParser.Field field, string typeName, ref string splitOn, ref List<Type> queryTypes,
        bool isNested = false)
    {
        var complexType = TypeBuilderExtension.GetTypeBuilder(typeName);
        complexType.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);

        if (field.Fields.Count > 0)
            splitOn += field.Fields[0].Name.Split(".").Last() + ", ";

        foreach (var complexField in field.Fields)
            if (complexField.Fields.Count > 0)
            {
                var nestedType = BuildComplexType(complexField, typeName + complexField.Type, ref splitOn,
                    ref queryTypes, field.Fields.Any());

                var attributesList = new List<CustomAttributeBuilder>();
                if (complexField.IsArray)
                {
                    var ctor = typeof(IsArrayAttribute).GetConstructors().First();

                    var customAttributeBuilder = new CustomAttributeBuilder(ctor!, new object[] { });
                    attributesList.Add(customAttributeBuilder);
                }

                if (isNested)
                {
                    var isNestedCtor = typeof(NestedTypeAttribute).GetConstructors().First();
                    var customAttributeIsNestedTypeBuilder =
                        new CustomAttributeBuilder(isNestedCtor!, new object[] { });

                    attributesList.Add(customAttributeIsNestedTypeBuilder);
                }


                TypeBuilderExtension.CreateProperty(complexType, complexField.Name, nestedType, attributesList);
            }
            else
            {
                var propertyType = ParseType(complexField.Type);
                TypeBuilderExtension.CreateProperty(complexType, complexField.Name, propertyType);
            }

        var createdType = complexType.CreateType();
        if (!queryTypes.Contains(createdType)) queryTypes.Insert(0, createdType);

        return createdType;
    }

    public Type BuildQueryResultType(Type type)
    {
        var resultType = TypeBuilderExtension.GetTypeBuilder(type.Name + "Result");
        resultType.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
                                            MethodAttributes.RTSpecialName);

        var properties = type.GetProperties();
        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;

            if (property.CustomAttributes.Select(c => c.AttributeType).Contains(typeof(IsArrayAttribute)))
            {
                var resultGenericType = BuildQueryResultType(propertyType);
                propertyType = typeof(List<>).MakeGenericType(resultGenericType);
            }
            else if (propertyType.IsClass && propertyType != typeof(string))
            {
                propertyType = BuildQueryResultType(propertyType);
            }

            TypeBuilderExtension.CreateProperty(resultType, property.Name, propertyType);
        }

        var result = resultType.CreateType();
        return result;
    }
}