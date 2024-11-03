using System.Diagnostics;
using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Entities;
using EntityArchitect.Entities.Context;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Entities.Repository;
using EntityArchitect.Results.Abstracts;

namespace EntityArchitect.CRUD;

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
    private DelegateBuilder(IServiceProvider provider) => _provider = provider;

    public static DelegateBuilder<TE, TEcrq, TEurq, TErs,TLlr> Create<TE, TEcrq, TEurq, TErs, TLlr>(IServiceProvider provider)
        where TE : Entity
        where TEcrq : class, new()
        where TEurq : class, new()
        where TErs : EntityResponse, new()
         => new(provider);

    public Func<TEntityCreateRequest, CancellationToken, ValueTask<Result<TEntityResponse>>> PostDelegate =>
        async (body, cancellationToken) =>
        {
            var entity = body.ConvertRequestToEntity<TEntity, TEntityCreateRequest>();
            var sql = CrudSqlBuilder.BuildPostSql(entity, _entityName);
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                await service.ExecuteSqlAsync(sql, cancellationToken);
            }
            var res = entity.ConvertEntityToResponse<TEntity, TEntityResponse>();
            return Result.Success(res);
        };

    public Func<TEntityUpdateRequest, CancellationToken, ValueTask<Result<TEntityResponse>>> UpdateDelegate =>
        async (body, cancellationToken) =>
        {
            var entity = body.ConvertRequestToEntity<TEntity, TEntityUpdateRequest>();
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                service.Update(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var res = entity.ConvertEntityToResponse<TEntity, TEntityResponse>();
            return Result.Success(res);
        };

    public Func<Guid, CancellationToken, ValueTask<Result>> DeleteDelegate =>
        async (id, cancellationToken) =>
        {
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var entity = await service.GetByIdAsync(id, cancellationToken);
                if (entity is null)
                    return Result.Failure(Error.NotFound(id, _entityName));

                service.Remove(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        };

    public Func<Guid, CancellationToken, ValueTask<Result<TEntityResponse>>> GetByIdDelegate =>
        async (id, cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            
            var properties = typeof(TEntity).GetProperties()
                .Where(x => x.CustomAttributes.Any(c => c.AttributeType == typeof(IncludeInGetAttribute)))
                .Select(x => x.Name)
                .ToList();
            
            var spec = new SpecificationGetById<TEntity>(x => x.Id == id, properties);
            
            var entity = await service.GetBySpecificationIdAsync(spec, cancellationToken);
            var response = entity is null
                ? Result.Failure<TEntityResponse>(Error.NotFound(id, _entityName))
                : entity.ConvertEntityToResponse<TEntity, TEntityResponse>();

            return response;
        };

    public Func<CancellationToken, ValueTask<Result<List<TLightListResponse>>>> GetLightListDelegate =>
        async (cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            
            var entities = await service.GetLightListAsync(cancellationToken);
            var response
                = entities.Select(c =>
                        c.ConvertEntityToLightListResponse<TEntity, TLightListResponse>())
                    .ToList();

            return response;
        };
}