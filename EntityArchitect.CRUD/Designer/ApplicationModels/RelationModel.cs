namespace EntityArchitect.CRUD.Designer.ApplicationModels;

public class RelationModel
{
    public RelationType Type { get; set; }
    public string TargetEntity { get; set; } 
    public string TargetPropertyName { get; set; }
    public bool CheckIfExists { get; set; } = true;
}