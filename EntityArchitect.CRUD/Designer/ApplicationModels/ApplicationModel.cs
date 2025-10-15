namespace EntityArchitect.CRUD.Designer.ApplicationModels;

public class ApplicationModel
{
    public List<EntityModel> Entities { get; set; } = new();
    public List<SqlModel> SqlModels { get; set; } = new();
}