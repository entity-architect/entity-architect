using System;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Entities.Attributes;

public class OneToOneAttribute<T>(string PropertyName, bool checkIfExists = true) 
    : Attribute where T : Entity;
