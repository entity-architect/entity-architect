using System;

namespace EntityArchitect.CRUD.Attributes.CrudAttributes;

public class IncludeInGetAttribute(int includingDeep = 0) : Attribute;