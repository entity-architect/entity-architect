namespace EntityArchitect.CRUD.Designer.ApplicationModels;

public class PropertyModel
{
    public string Name { get; set; } 
    public string Type { get; set; }
    
    // Request Attributes
    public bool IgnorePostRequest { get; set; }
    public bool IgnorePutRequest { get; set; }
    
    // Get Attributes
    public bool IncludeInGet { get; set; }
    public int? IncludingDeep { get; set; }
    
    // Light List Attribute
    public bool LightListProperty { get; set; }
    
    // Relation
    public RelationModel? Relation { get; set; }
}