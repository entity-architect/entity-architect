using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Authorization.Requests;

public class AuthorizationRequest : EntityRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}