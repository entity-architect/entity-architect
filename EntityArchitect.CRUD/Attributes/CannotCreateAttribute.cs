using System.Linq.Expressions;

namespace EntityArchitect.CRUD.Attributes;

public class CannotCreateAttribute : Attribute;
public class CannotUpdateAttribute : Attribute;
public class CannotDeleteAttribute : Attribute;
public class CannotGetByIdAttribute : Attribute;
public class SearchableAttribute(string properties) : Attribute;
public class HasLightListAttribute : Attribute;
public class HasPaginationAttribute(int pageSize, int itemSize) : Attribute;
public class HasFilterAttribute<TEntity>(Expression<Func<TEntity>> expression) : Attribute;
public class HasSortAttribute() : Attribute;
public class IncludeInGetAttribute : Attribute;

