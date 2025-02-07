using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Authorization.Responses;
using EntityArchitect.CRUD.Authorization.Service;
using EntityArchitect.CRUD.CustomEndpoints;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Helpers;
using EntityArchitect.CRUD.Middlewares;
using EntityArchitect.CRUD.Queries;
using EntityArchitect.CRUD.Results;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.TypeBuilders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntityArchitect.CRUD;

public static partial class ApiBuilder
{
    public static void Main()
    {
    }

    public static IApplicationBuilder MapEntityArchitectCrud(this IApplicationBuilder app, Assembly assembly,
        string basePath = "")
    {
        var enumerable = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        var typeBuilder = new TypeBuilder();
        foreach (var entity in enumerable)
        {
            var result = ConvertEndpointNameRegex().Replace(entity.Name, "$1-$2");
            var name = result.ToLower();

            var authorizationPolicies = new List<Type>();
            var haveAuthorization = entity.CustomAttributes.Any(c => c.AttributeType == typeof(SecuredAttribute));
            var authorizationEntityAttribute = entity.GetCustomAttribute<SecuredAttribute>();
            if (authorizationEntityAttribute is not null)
            {
                foreach (var type in authorizationEntityAttribute.EntityTypes)
                {
                    if (type.CustomAttributes.All(c => c.AttributeType != typeof(AuthorizationEntityAttribute)))
                        throw new Exception(
                            $"AuthorizationEntityAttribute can only have AuthorizationEntityAttribute as EntityTypes. {type.Name}");
                    
                    authorizationPolicies.Add(type);
                }
            }

            var requestPostType = typeBuilder.BuildCreateRequestFromEntity(entity);
            var requestUpdateType = typeBuilder.BuildUpdateRequestFromEntity(entity);
            var responseType = typeBuilder.BuildResponseFromEntity(entity);
            var lightListResponseType = typeBuilder.BuildLightListProperty(entity);

            app.UseRouting();
            app.UseEndpoints(async endpoints =>
            {
                app.UseMiddleware<ClaimProviderMiddleware>();

                var auth = endpoints.ServiceProvider.GetService(typeof(IAuthorizationBuilderService));
                if (auth is not null)
                {
                    app.UseMiddleware<AuthorizationMiddleware>();
                    app.UseAuthentication();
                    app.UseAuthorization();
                }

                var group = endpoints.MapGroup(Path.Combine(basePath, name));

                var delegateBuilder = typeof(DelegateBuilder<,,,,>)
                    .MakeGenericType(entity, requestPostType, requestUpdateType, responseType, lightListResponseType)
                    .GetMethod("Create")?
                    .MakeGenericMethod(entity, requestPostType, requestUpdateType, responseType, lightListResponseType)
                    .Invoke(null, new object[] { endpoints.ServiceProvider });

                if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotCreateAttribute)))
                {
                    var postHandler =
                        delegateBuilder!.GetType().GetProperty("PostDelegate")!.GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapPost("", postHandler!);

                    if (haveAuthorization)
                        endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

                    endpoint.WithSummary($"Create {entity.Name}");
                    endpoint.WithDisplayName($"Create {entity.Name}");
                    endpoint.Produces(200, typeof(Result<>).MakeGenericType(responseType));
                    endpoint.Produces(400, typeof(Result));
                    endpoint.Produces(500, typeof(Result));
                }

                if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotUpdateAttribute)))
                {
                    var updateHandler =
                        delegateBuilder!.GetType().GetProperty("UpdateDelegate")!.GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapPut("", updateHandler!);

                    if (haveAuthorization)
                        endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

                    endpoint.WithSummary($"Update {entity.Name}");
                    endpoint.WithDisplayName($"Update {entity.Name}");
                    endpoint.Produces(200, typeof(Result<>).MakeGenericType(responseType));
                    endpoint.Produces(404, typeof(Result));
                    endpoint.Produces(500, typeof(Result));
                }

                if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotDeleteAttribute)))
                {
                    var deleteHandler =
                        delegateBuilder!.GetType().GetProperty("DeleteDelegate")!.GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapDelete("{id}", deleteHandler!);

                    if (haveAuthorization)
                        endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

                    endpoint.WithSummary($"Delete {entity.Name} by Id");
                    endpoint.WithDisplayName($"Delete {entity.Name} by Id");
                    endpoint.Produces(200, typeof(Result));
                    endpoint.Produces(404, typeof(Result));
                    endpoint.Produces(500, typeof(Result));
                    endpoint.WithOpenApi(op =>
                    {
                        op.Parameters.First(c => c.Name == "id").Description = "Id of the entity";
                        return op;
                    });
                }

                if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotGetByIdAttribute)))
                {
                    var getByIdHandler =
                        delegateBuilder!.GetType().GetProperty("GetByIdDelegate")!
                            .GetValue(delegateBuilder) as Delegate;

                    var endpoint = group.MapGet("{id}", getByIdHandler!);

                    if (haveAuthorization)
                        endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

                    endpoint.WithSummary($"Get {entity.Name} by Id");
                    endpoint.WithDisplayName($"Get {entity.Name} by Id");
                    endpoint.Produces(200, typeof(Result<>).MakeGenericType(responseType));
                    endpoint.Produces(404, typeof(Result));
                    endpoint.Produces(500, typeof(Result));
                    endpoint.WithOpenApi(op =>
                    {
                        op.Parameters.First(c => c.Name == "id").Description = "Id of the entity";
                        return op;
                    });
                }

                if (entity.CustomAttributes.Any(c => c.AttributeType == typeof(HasLightListAttribute)))
                {
                    var lightListProperties =
                        entity.GetProperties()
                            .Where(c => c.CustomAttributes
                                .Select(attributeData => attributeData.AttributeType)
                                .Contains(typeof(LightListPropertyAttribute)))
                            .Select(c => c.Name)
                            .ToList();

                    var getLightListDelegate =
                        delegateBuilder!.GetType().GetProperty("GetLightListDelegate")!.GetValue(delegateBuilder) as
                            Delegate;

                    var endpoint = group.MapGet("light-list", getLightListDelegate!);
                    endpoint.WithSummary(
                        $"Get light list of {entity.Name}s. Only includes Id and {string.Join(",", lightListProperties)}");

                    if (haveAuthorization)
                        endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());
                }

                if (entity.CustomAttributes.Any(c => c.AttributeType == typeof(GetListPaginatedAttribute)))
                {
                    var getLightListDelegate =
                        delegateBuilder!.GetType().GetProperty("GetListDelegate")!
                            .GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapGet("list/{page}", getLightListDelegate!);

                    if (haveAuthorization)
                        endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

                    endpoint.WithSummary($"Get list of {entity.Name}s paginated");
                    endpoint.WithOpenApi(op =>
                    {
                        op.Parameters.First(c => c.Name == "page").Description = "Page number, indexing starts from 0";
                        return op;
                    });
                    endpoint.WithDisplayName($"Get paginated list of {entity.Name}s");
                    endpoint.Produces(200,
                        typeof(Result<>).MakeGenericType(typeof(PaginatedResult<>).MakeGenericType(responseType)));
                    endpoint.Produces(500, typeof(Result));
                }

                if (entity.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationEntityAttribute)))
                {
                    var loginDelegate =
                        delegateBuilder!.GetType().GetProperty("Login")!
                            .GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapPost("login", loginDelegate!);
                    endpoint.WithSummary($"Login {entity.Name}");

                    endpoint.WithDisplayName($"Login user of type {entity.Name}");
                    endpoint.Produces(200,
                        typeof(Result<>).MakeGenericType(typeof(AuthorizationResponse)));
                    endpoint.Produces(500, typeof(Result));

                    var refreshTokenDelegate =
                        delegateBuilder!.GetType().GetProperty("RefreshToken")!
                            .GetValue(delegateBuilder) as Delegate;
                    endpoint = group.MapPost("refresh-token", refreshTokenDelegate!);
                    endpoint.WithSummary($"Refresh token {entity.Name}");
                    endpoint.AllowAnonymous();
                    endpoint.RequireAuthorization(entity.Name);

                    endpoint.WithDisplayName($"Refresh token for user of type {entity.Name}");
                    endpoint.Produces(200,
                        typeof(Result<>).MakeGenericType(typeof(AuthorizationResponse)));
                    endpoint.Produces(500, typeof(Result));
                }

                var queries = assembly.GetTypes().Where(c => c.BaseType == typeof(Query<>).MakeGenericType(entity));
                foreach (var query in queries)
                {
                    var instance = Activator.CreateInstance(query);
                    var sql = query.GetProperty(nameof(Query<Entity>.Sql))?.GetValue(instance);
                    if (sql is null) continue;

                    if ((bool)query.GetProperty(nameof(Query<Entity>.UseSqlFile))?.GetValue(instance)!)
                        sql = await File.ReadAllTextAsync((string)sql);

                    var queryType = typeBuilder.BuildQueryRequest((sql as string)!, query.Name);
                    var mi = typeof(ApiBuilder)
                        .GetMethod("MapGetEndpoint")?
                        .MakeGenericMethod(queryType, query, entity);

                    mi!.Invoke(group, new object[] { group, query.Name, app });
                }

                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var customEndpoints = typeof(CustomEndpointBuilder).GetMethod(nameof(CustomEndpointBuilder.Build))
                        .MakeGenericMethod(entity)
                        .Invoke(null, new object[] { assembly, scope });

                    var endpointListType = typeof(List<>).MakeGenericType(typeof(CustomEndpoint<>).MakeGenericType(entity));
                    var customEndpointList = Convert.ChangeType(customEndpoints, endpointListType) as IEnumerable<object>;

                    if (customEndpointList != null && customEndpointList.Any())
                    {
                        foreach (var customEndpoint in customEndpointList)
                        {
                            foreach (var method in customEndpoint.GetType().GetMethods())
                            {
                                if (method.CustomAttributes.All(c =>
                                        c.AttributeType != typeof(CustomEndpointAttribute)))
                                    continue;

                                var customEndpointAttribute =
                                    method.GetCustomAttribute<CustomEndpointAttribute>();
                                string httpMethod = customEndpointAttribute.Method;
                                var name = customEndpointAttribute.Name;
                                
                                switch (httpMethod)
                                {
                                    case "POST":
                                    {
                                        var parameters = method.GetParameters();

                                        var endpoint = group.MapPost(name, async (HttpContext context, IServiceProvider services) =>
                                        {
                                            var service = services.GetRequiredService(customEndpoint.GetType());

                                            var args = new object?[parameters.Length];

                                            for (int i = 0; i < parameters.Length; i++)
                                            {
                                                var param = parameters[i];
                                                var paramValue = await context.Request.ReadFromJsonAsync(param.ParameterType);
                                                args[i] = paramValue;
                                            }

                                            var result = method.Invoke(service, args);

                                            if (result is Task task)
                                            {
                                                await task;
                                                var resultProperty = task.GetType().GetProperty("Result");
                                                result = resultProperty?.GetValue(task);
                                            }

                                            return result;
                                        });
                                        
                                        if(method.CustomAttributes.Any(c => c.AttributeType == typeof(SecuredAttribute)))
                                            endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());
                                        
                                        endpoint.Produces(200, typeof(Result<>).MakeGenericType(method.ReturnType));
                                        endpoint.Produces(400, typeof(Result));
                                        endpoint.Produces(500, typeof(Result));
                                        if (parameters.Length > 0)
                                        {
                                            var requestBodyType = parameters.First().ParameterType;
                                            endpoint.Accepts(requestBodyType, "application/json");
                                        }
                                        break;
                                    }
                                    default:
                                        throw new Exception($"HttpMethod {httpMethod} not supported now.");
                                }
                            }
                        }
                    }
                }
                group.WithTags(name);
            });
        }

        return app;
    }


    public static void MapGetEndpoint<TParam, TQuery, TEntity>(IEndpointRouteBuilder group, string endpointName,
        IApplicationBuilder app)
        where TEntity : Entity
        where TQuery : Query<TEntity>
        where TParam : class
    {
        QueryHandler<TParam, TEntity> queryHandler = new();
        var query = Activator.CreateInstance<TQuery>();
        var result = ConvertEndpointNameRegex().Replace(endpointName, "$1-$2");
        
        var endpoint = group.MapGet(result.ToLower(), ([AsParameters] TParam param) =>
        {
            var context = app.ApplicationServices.GetService<IConfiguration>();
            var connectionString = context.GetConnectionString("DefaultConnection");
            var result = queryHandler.HandleAsync(query, param, connectionString, default);
            return result;
        });
        
        var authorizationPolicies = new List<Type>();
        var haveAuthorization = typeof(TQuery).CustomAttributes.Any(c => c.AttributeType == typeof(SecuredAttribute));
        var authorizationEntityAttribute = typeof(TQuery).GetCustomAttribute<SecuredAttribute>();
        if (authorizationEntityAttribute is not null)
        {
            foreach (var type in authorizationEntityAttribute.EntityTypes)
            {
                if(type.BaseType != typeof(SecuredAttribute))
                    throw new Exception($"AuthorizationEntityAttribute can only have AuthorizationEntityAttribute as EntityTypes. {type.Name}");
                        
                authorizationPolicies.Add(type);
            }
        }
        
        if(haveAuthorization)
            endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex ConvertEndpointNameRegex();
}