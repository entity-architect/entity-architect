using System;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class OneToManyAttribute<T>(string PropertyName, bool checkIfExists = true) : Attribute where T : Entity
{
    public string EntityName { get; set; }
    
    public OneToManyAttribute(string propertyName, string entityName, bool checkIfExists = true) : this(propertyName, checkIfExists)
    {
        EntityName = entityName;
    }
}