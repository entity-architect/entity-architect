using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Application;

public class Endpoints : Entity
{
    public int Number { get; set; }
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
}