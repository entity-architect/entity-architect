using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class RelationManyToOneAttribute<T>(string PropertyName) : Attribute where T : Entity;