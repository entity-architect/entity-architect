using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

public class OneToOneAttribute<T>(string PropertyName) : Attribute where T : Entity;
