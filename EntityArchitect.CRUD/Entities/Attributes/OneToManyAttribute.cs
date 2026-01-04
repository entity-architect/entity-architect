using System;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

/// <summary>
/// Non-generic version for dynamic type building
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OneToManyAttribute(string propertyName, string entityName, bool checkIfExists = true) : Attribute
{
    public string PropertyName { get; } = propertyName;
    public string EntityName { get; } = entityName;
    public bool CheckIfExists { get; } = checkIfExists;
}

/// <summary>
/// Generic version for compile-time type safety
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class OneToManyAttribute<T>(string propertyName, bool checkIfExists = true) : OneToManyAttribute(propertyName, typeof(T).Name, checkIfExists) where T : Entity;