using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class RelationOneToManyAttribute<T>(string PropertyName,
    bool checkIfExists = true) : Attribute where T : Entity;