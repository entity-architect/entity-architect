using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Authorization.Requests;
using EntityArchitect.CRUD.Authorization.Responses;
using EntityArchitect.CRUD.Authorization.Service;
using EntityArchitect.CRUD.Entities.Attributes;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.TypeBuilders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

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

    public Func<TEntityCreateRequest, CancellationToken, ValueTask<Result<TEntityResponse>>> PostDelegate =>
        async (body, cancellationToken) =>
        {
            var entity = body.ConvertRequestToEntity<TEntity, TEntityCreateRequest>();

            foreach (var item in entity.GetType().GetProperties()
                         .Where(c =>
                             c.PropertyType.BaseType == typeof(Entity) &&
                             c.CustomAttributes.Any(x =>
                                 x.AttributeType == typeof(RelationOneToManyAttribute<>)
                                     .MakeGenericType(c.PropertyType))))
            {
                var entityId = (item.GetValue(entity) as Entity)!.Id.Value;
                var repositoryType = typeof(IRepository<>).MakeGenericType(item.PropertyType);
                using var scope = _provider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService(repositoryType);

                var result = repository.GetType().GetMethod(nameof(IRepository<Entity>.ExistsAsync))!
                    .Invoke(repository, new object[] { entityId!, cancellationToken });

                if (!await (result as Task<bool>)!)
                {
                    return Result.Failure<TEntityResponse>(Error.NotFound(entityId, item.PropertyType.Name));
                }
            }
            
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

            return entity.ConvertEntityToResponse<TEntity, TEntityResponse>();
        };

    public Func<TEntityUpdateRequest, CancellationToken, ValueTask<Result<TEntityResponse?>>> UpdateDelegate =>
        async (body, cancellationToken) =>
        {
            var entity = body.ConvertRequestToEntity<TEntity, TEntityUpdateRequest>();
            using (var scope = _provider.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var actions = scope.GetEndpointActionsAsync<TEntity>();
                entity = await actions.InvokeBeforePutAsync(entity, cancellationToken);

                var oldEntity = await service.GetByIdAsync(new Id<TEntity?>(entity.Id.Value), cancellationToken);
                if (oldEntity is null)
                    return Result.Failure<TEntityResponse>(Error.NotFound(entity.Id.Value, _entityName));

                oldEntity = entity;
                
                await unitOfWork.SaveChangesAsync(cancellationToken);
                entity = await actions!.InvokeAfterPutAsync(entity, cancellationToken);
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
                var actions = scope.GetEndpointActionsAsync<TEntity>();

                var entity = await service.GetByIdAsync(id, cancellationToken);
                if (entity is null)
                    return Result.Failure(Error.NotFound(id, _entityName));
                entity = await actions.InvokeBeforeDeleteAsync(entity, cancellationToken);

                service.Remove(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await actions!.InvokeAfterDeleteAsync(entity, cancellationToken);
            }

            return Result.Success();
        };

    public Func<Guid, CancellationToken, ValueTask<Result<TEntityResponse>>> GetByIdDelegate =>
        async (id, cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            var actions = scope.GetEndpointActionsAsync<TEntity>();

            var properties = typeof(TEntity).GetProperties()
                .Where(x => x.CustomAttributes.Any(c => c.AttributeType == typeof(IncludeInGetAttribute)))
                .Select(x => x.Name)
                .ToList();

            var spec = new SpecificationBySpec<TEntity>(x => x.Id == id, properties);

            var entity = await service.GetBySpecificationIdAsync(spec, cancellationToken);
            if (entity is not null)
            {
                entity = await actions.InvokeAfterGetByIdAsync(entity, cancellationToken);
                return entity.ConvertEntityToResponse<TEntity, TEntityResponse>();
            }
            
            var result = Result.Failure<TEntityResponse>(Error.NotFound(id, _entityName));
            return result;
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
    
    public Func<int, CancellationToken, ValueTask<Result<PaginatedResult<TEntityResponse>>>> GetListDelegate =>
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
            return paginatedResponse;
        };
    
    public Func<AuthorizationRequest, CancellationToken, ValueTask<Result<AuthorizationResponse>>> Login =>
        async ([FromBody] loginRequest, cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationBuilderService>();
            
            var usernameProperty = typeof(TEntity).GetProperties().FirstOrDefault(c => c.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationUsernameAttribute)));
            if(usernameProperty is null)
                return Result.Failure<AuthorizationResponse>(new Error(HttpStatusCode.NotFound, "Username property not found in entity."));
            
            var passwordProperty = typeof(TEntity).GetProperties().FirstOrDefault(c => c.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationPasswordAttribute)));
            if(passwordProperty is null)
                return Result.Failure<AuthorizationResponse>(new Error(HttpStatusCode.NotFound, "Password property not found in entity."));
            
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var usernamePropertyExpression = Expression.Property(parameter, usernameProperty.Name);

            var usernameCondition = Expression.Equal(
                usernamePropertyExpression,
                Expression.Constant(loginRequest.Username));
            
            var lambda = Expression.Lambda<Func<TEntity, bool>>(usernameCondition, parameter);

            var specification = new SpecificationBySpec<TEntity>(lambda, []);
            var entities = await repository.GetBySpecificationAsync(specification, cancellationToken);
            if(entities.Count == 0)
                return Result.Failure<AuthorizationResponse>(new Error(HttpStatusCode.NotFound, $"User {_entityName} not found."));
            
            var entity = entities.First();
            var password = passwordProperty.GetValue(entity)?.ToString();
            
            return !BCrypt.Net.BCrypt.Verify(loginRequest.Password, password) ? 
                Result.Failure<AuthorizationResponse>(new Error(HttpStatusCode.Unauthorized, "Invalid password.")) : 
                authService.CreateAuthorizationToken(entity)!;
        };
    
    public Func<string, CancellationToken, ValueTask<Result<AuthorizationResponse>>> RefreshToken =>
        async (refreshRequest, cancellationToken) =>
        {
            using var scope = _provider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationBuilderService>();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<TEntity>>();
            
            var handler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(scope.ServiceProvider.GetRequiredService<IConfiguration>().GetSection("Jwt:RefreshKey").Value)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true
            };
            var identity = await handler.ValidateTokenAsync(refreshRequest, tokenValidationParameters);

            if(!identity.IsValid)
                return Result.Failure<AuthorizationResponse>(new Error(HttpStatusCode.Unauthorized, "Invalid token."));
            var id = Guid.Parse(identity.Claims.First(c => c.Key == "id").Value.ToString() ?? string.Empty); 
            
            var entity = await repository.GetByIdAsync(new Id<TEntity>(id), cancellationToken);
            if(entity is null)
                return Result.Failure<AuthorizationResponse>(new Error(HttpStatusCode.NotFound, $"User {_entityName} not found."));
            

            var response = authService.CreateAuthorizationToken(entity);
            return response!;
        };
    
    
}