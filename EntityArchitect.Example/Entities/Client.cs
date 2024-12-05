using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Example.Entities;

[GetListPaginated(3)]
public class Client : Entity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    
    [RelationManyToOne<Rental>(nameof(Rental.Client)), IncludeInGet(1), IgnorePostRequest, IgnorePutRequest]
    public List<Rental> Rentals { get; private set; }
}