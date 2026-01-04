using System;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

/// <summary>
/// Non-generic version for dynamic type building
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ManyToOneAttribute(string propertyName, string entityName) : Attribute
{
    public string PropertyName { get; } = propertyName;
    public string EntityName { get; } = entityName;
}

/// <summary>
/// Generic version for compile-time type safety
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ManyToOneAttribute<T>(string propertyName) : ManyToOneAttribute(propertyName, typeof(T).Name) where T : Entity;