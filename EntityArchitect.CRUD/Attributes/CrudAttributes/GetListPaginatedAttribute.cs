using System;

namespace EntityArchitect.CRUD.Attributes.CrudAttributes;

public class GetListPaginatedAttribute(int itemCount = 10) : Attribute;