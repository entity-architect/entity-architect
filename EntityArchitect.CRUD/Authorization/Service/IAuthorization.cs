using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD.Authorization.Service;

public interface IAuthorization
{
    AuthorizationResponse CreateAuthorizationToken<TAuthorizationEntity>(TAuthorizationEntity entity) 
        where TAuthorizationEntity : Entity;
}