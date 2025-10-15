using System;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ManyToOneAttribute<T>(string PropertyName) : Attribute where T : Entity
{
    public string EntityName { get; set; }
    
    public ManyToOneAttribute(string propertyName, string entityName) : this(propertyName)
    {
        EntityName = entityName;
    }
}