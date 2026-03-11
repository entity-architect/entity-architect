namespace EntityArchitect.CRUD.Designer.ApplicationModels;

public class EntityModel
{
    public string Name { get; set; }
    public List<PropertyModel> Properties { get; set; } = new();
    
    // CRUD Attributes
    public bool CannotCreate { get; set; }
    public bool CannotPost { get; set; } // alias for CannotCreate
    public bool CannotUpdate { get; set; }
    public bool CannotPut { get; set; } // alias for CannotUpdate
    public bool CannotDelete { get; set; }
    public bool CannotGetById { get; set; }
    
    // List Attributes
    public bool HasLightList { get; set; }
    public bool GetListPaginated { get; set; }
    public int? PaginatedItemCount { get; set; }
}