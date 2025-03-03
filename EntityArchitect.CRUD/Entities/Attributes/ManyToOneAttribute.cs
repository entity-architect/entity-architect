using System;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ManyToOneAttribute<T>(string PropertyName) : Attribute where T : Entity;