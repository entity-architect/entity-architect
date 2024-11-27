using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Entities;
using EntityArchitect.Entities.Context;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Entities.Repository;
using EntityArchitect.Results;
using EntityArchitect.Results.Abstracts;
using Microsoft.AspNetCore.Mvc;

namespace EntityArchitect.CRUD.Helpers;

public class DelegateBuilder<
    TEntity,
    TEntityCreateRequest,
    TEntityUpdateRequest,
    TEntityResponse,
    TLightListResponse>
    where TEntity : Entity
    where TEntityResponse : EntityResponse, new()
{
    private readonly IServiceProvider _provider;
    private readonly string _entityName = typeof(TEntity).Name;
    private DelegateBuilder(IServiceProvider provider) =>
        _provider = provider;

    public static DelegateBuilder<TE, TEcRq, TEuRq, TErs, TLlr> 
        Create<TE, TEcRq, TEuRq, TErs, TLlr>(
        IServiceProvider provider)
        where TE : Entity
        where TEcRq : class, new()
        where TEuRq : class, new()
        where TErs : EntityResponse, new()
        => new(provider);

    public Func<TEntityCreateRequest, CancellationToken, ValueTask<IActionResult>> PostDelegate =>
        async (body, cancellationToken) =>
        {
            var entity = body.ConvertRequestToEntity<TEntity, TEntityCreateRequest>();
            var sql = CrudSqlBuilder.BuildPostSql(entity, _entityName);
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var actions =scope.GetEndpointActionsAsync<TEntity>();
                entity.SetCreatedDate();
                entity = await actions!.InvokeBeforePostAsync(entity, cancellationToken);
                  await service.ExecuteSqlAsync(sql, cancellationToken);
                entity = await actions!.InvokeAfterPostAsync(entity, cancellationToken);
            }

            var res = entity.ConvertEntityToResponse<TEntity, TEntityResponse>();
            return new OkObjectResult(Result.Success(res));
        };

    public Func<TEntityUpdateRequest, CancellationToken, ValueTask<IActionResult>> UpdateDelegate =>
        async (body, cancellationToken) =>
        {
            var entity = body.ConvertRequestToEntity<TEntity, TEntityUpdateRequest>();
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var actions = scope.GetEndpointActionsAsync<TEntity>();
                entity = await actions!.InvokeBeforePutAsync(entity, cancellationToken);

                var oldEntity = await service.GetByIdAsync(new Id<TEntity?>(entity.Id.Value), cancellationToken);
                if (oldEntity is null)
                    return new NotFoundObjectResult(Result.Failure(Error.NotFound(entity.Id.Value, _entityName)));

                oldEntity = entity;
                
                await unitOfWork.SaveChangesAsync(cancellationToken);
                entity = await actions!.InvokeAfterPutAsync(entity, cancellationToken);
            }

            var res = entity.ConvertEntityToResponse<TEntity, TEntityResponse>();
            return new OkObjectResult(Result.Success(res));
        };

    public Func<Guid, CancellationToken, ValueTask<IActionResult>> DeleteDelegate =>
        async (id, cancellationToken) =>
        {
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var actions = scope.GetEndpointActionsAsync<TEntity>();

                var entity = await service.GetByIdAsync(id, cancellationToken);
                if (entity is null)
                    return new NotFoundObjectResult(Result.Failure(Error.NotFound(id, _entityName)));
                entity = await actions.InvokeBeforeDeleteAsync(entity, cancellationToken);

                service.Remove(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await actions!.InvokeAfterDeleteAsync(entity, cancellationToken);
            }

            return new OkObjectResult(Result.Success());
        };

    public Func<Guid, CancellationToken, ValueTask<IActionResult>> GetByIdDelegate =>
        async (id, cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            var actions = scope.GetEndpointActionsAsync<TEntity>();

            var properties = typeof(TEntity).GetProperties()
                .Where(x => x.CustomAttributes.Any(c => c.AttributeType == typeof(IncludeInGetAttribute)))
                .Select(x => x.Name)
                .ToList();

            var spec = new SpecificationGetById<TEntity>(x => x.Id == id, properties);

            var entity = await service.GetBySpecificationIdAsync(spec, cancellationToken);
            if (entity is not null)
            {
                entity = await actions.InvokeAfterGetByIdAsync(entity, cancellationToken);
                return new OkObjectResult(Result.Success(entity.ConvertEntityToResponse<TEntity, TEntityResponse>()));
            }
            
            var result = Result.Failure(Error.NotFound(id, _entityName));
            return new NotFoundObjectResult(result);
        };

    public Func<CancellationToken, ValueTask<IActionResult>> GetLightListDelegate =>
        async (cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            
            var entities = await service.GetLightListAsync(cancellationToken);
            
            var response
                = entities.Select(c =>
                        c.ConvertEntityToLightListResponse<TEntity, TLightListResponse>())
                    .ToList();

            return new OkObjectResult(response);
        };
    
    public Func<int, CancellationToken, ValueTask<IActionResult>> GetListDelegate =>
        async (page, cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            var actions = scope.GetEndpointActionsAsync<TEntity>();

            var itemCount = (int)typeof(TEntity).CustomAttributes
                .First(c => c.AttributeType == typeof(GetListPaginatedAttribute)).ConstructorArguments.First().Value!;

            var properties = typeof(TEntity).GetProperties()
                .Where(x => x.CustomAttributes.Any(c => c.AttributeType == typeof(IncludeInGetAttribute)))
                .Select(x => x.Name)
                .ToList();

            var entities = await service.GetAllPaginatedAsync(page, itemCount, properties, cancellationToken);
            entities = await actions.InvokeAfterGetPaginatedAsync(page, itemCount, entities, cancellationToken);
            var response
                = entities.Select(c =>
                        c.ConvertEntityToResponse<TEntity, TEntityResponse>())
                    .ToList();
            
            var totalCount = await service.GetCountAsync(cancellationToken);
            var pageCount = (int)Math.Round((double)totalCount / itemCount, MidpointRounding.ToEven);
            var leftPages = pageCount - (page + 1);
            if(pageCount == 0) 
                leftPages = 0;
            
            var paginatedResponse = new PaginatedResult<TEntityResponse>(response, page, leftPages, pageCount, totalCount);
            return new  OkObjectResult(paginatedResponse);
        };
}