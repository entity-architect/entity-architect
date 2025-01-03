using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD.Authorization.Service;

public interface IAuthorizationBuilderService
{
    AuthorizationResponse CreateAuthorizationToken<TAuthorizationEntity>(TAuthorizationEntity entity) 
        where TAuthorizationEntity : Entity;
}