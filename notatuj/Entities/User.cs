using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Entities;

namespace notatuj.Entities;

[AuthorizationEntity]
[CannotGetById]

public class User : Entity
{
    [AuthorizationUsername] public string Email { get; set; }
    [AuthorizationPassword] public string Password { get; set; }
    
    public string FirstName { get; set; }
    public string LastName { get; set; }
    
    [ManyToOne<Contribution>(nameof(Contribution.User)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Contribution> Notes { get; set; }
    
    [ManyToOne<Likes>(nameof(Entities.Likes.User)), IgnorePutRequest, IgnorePostRequest]
    public ICollection<Likes> Likes { get; set; }
}