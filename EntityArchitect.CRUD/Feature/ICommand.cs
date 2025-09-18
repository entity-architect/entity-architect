using EntityArchitect.CRUD.Entities.Entities;

namespace EntityArchitect.CRUD.CustomEndpoints;

public interface ICommand<TEntity, TResponse> where TEntity : IEntity; 