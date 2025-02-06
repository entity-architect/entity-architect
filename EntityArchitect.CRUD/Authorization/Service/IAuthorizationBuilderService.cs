
using EntityArchitect.CRUD.Authorization.Responses;
using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.Authorization.Service;

public interface IAuthorizationBuilderService
{
    AuthorizationResponse CreateAuthorizationToken<TAuthorizationEntity>(TAuthorizationEntity entity) 
        where TAuthorizationEntity : Entity;
}