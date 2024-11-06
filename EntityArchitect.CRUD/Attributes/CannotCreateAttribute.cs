namespace EntityArchitect.CRUD.Attributes;

public class CannotCreateAttribute : Attribute;
public class CannotUpdateAttribute : Attribute;
public class CannotDeleteAttribute : Attribute;
public class CannotGetByIdAttribute : Attribute;
public class GetListPaginatedAttribute(int itemCount = 10) : Attribute;
public class SearchableAttribute(string properties) : Attribute;
public class HasLightListAttribute : Attribute;
public class LightListPropertyAttribute : Attribute;
public class IncludeInGetAttribute(int includingDeep = 0) : Attribute;

