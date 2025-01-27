using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.Example.Entities;

[GetListPaginated(3)]
public class Client : Entity
{
    public string Name { get; }
    public string Email { get; }

    [RelationManyToOne<Rental>(nameof(Rental.Client))]
    [IncludeInGet(1)]
    [IgnorePostRequest]
    [IgnorePutRequest]
    public List<Rental> Rentals { get; }
}