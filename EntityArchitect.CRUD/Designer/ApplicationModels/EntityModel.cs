namespace EntityArchitect.CRUD.Designer.ApplicationModels;

public class EntityModel
{
    public bool CannotPost { get; set; }
    public bool CannotPut { get; set; }
    public bool CannotDelete { get; set; }
    public string Name { get; set; }
    public List<PropertyModel> Properties { get; set; } = new();
}