namespace EntityArchitect.CRUD.Application;

public class EndpointMap()
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public ICollection<Access> Accesses { get; set; } = [];
    
}