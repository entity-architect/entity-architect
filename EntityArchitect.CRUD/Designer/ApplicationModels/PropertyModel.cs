namespace EntityArchitect.CRUD.Designer.ApplicationModels;

public class PropertyModel
{
    public bool IgnorePostRequest { get; set; }
    public bool IgnorePutRequest { get; set; }
    
    public string Name { get; set; } 
    public string Type { get; set; }
    
    public RelationModel? Relation { get; set; }
}