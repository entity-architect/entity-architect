using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.Authorization;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.Entities.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.Example.Entities;

[GetListPaginated(3), AuthorizationEntity]
public class Client : Entity
{
    public string Name { get; private set; }
    
    [AuthorizationUsername]
    public string Email { get; private set; }
    
    [AuthorizationPassword]
    public string Password { get; private set; }
    
    [RelationManyToOne<Rental>(nameof(Rental.Client)), IncludeInGet(1), IgnorePostRequest, IgnorePutRequest]
    public List<Rental> Rentals { get; private set; }
}