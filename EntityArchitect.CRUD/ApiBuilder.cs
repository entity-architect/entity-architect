using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Authorization.Service;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Feature;
using EntityArchitect.CRUD.Feature.Methods;
using EntityArchitect.CRUD.Files;
using EntityArchitect.CRUD.Helpers;
using EntityArchitect.CRUD.Queries;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using EntityArchitect.CRUD.TypeBuilders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RouteAttribute = EntityArchitect.CRUD.Feature.RouteAttribute;

namespace EntityArchitect.CRUD;

public static partial class ApiBuilder
{
    public static void Main()
    {
    }

    // Helper do łączenia segmentów tras (zawsze z '/')
    private static string JoinRoute(params string[] segments) =>
        "/" + string.Join('/', segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim('/')));

    public static IApplicationBuilder MapEntityArchitectCrud(this IApplicationBuilder app, Assembly assembly,
        string basePath = "")
    {
        var enumerable = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        var typeBuilder = new TypeBuilder();

        // Middleware MUSZĄ być dodane przed mapowaniem endpointów
        app.UseRouting();

        var auth = app.ApplicationServices.GetService(typeof(IAuthorizationBuilderService));
        if (auth is not null)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<AuthorizationMiddleware>();
        }

        app.UseAntiforgery();

        // Konfiguracja endpointów – synchroniczna
        void Configure(IEndpointRouteBuilder endpoints)
        {
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
                            throw new Exception($"AuthorizationEntityAttribute can only have AuthorizationEntityAttribute as EntityTypes. {type.Name}");

                        authorizationPolicies.Add(type);
                    }
                }

                var requestPostType = typeBuilder.BuildCreateRequestFromEntity(entity);
                var requestUpdateType = typeBuilder.BuildUpdateRequestFromEntity(entity);
                var responseType = typeBuilder.BuildResponseFromEntity(entity);
                var lightListResponseType = typeBuilder.BuildLightListProperty(entity);

                // ZAMIANA Path.Combine -> JoinRoute
                var group = endpoints.MapGroup(JoinRoute(basePath, name));

                var delegateBuilder = typeof(DelegateBuilder<,,,,>).MakeGenericType(entity, requestPostType, requestUpdateType, responseType, lightListResponseType)
                    .GetMethod("Create")
                    ?.MakeGenericMethod(entity, requestPostType, requestUpdateType, responseType, lightListResponseType)
                    .Invoke(null, new object[] { endpoints.ServiceProvider });

                if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotCreateAttribute)))
                {
                    var postHandler = delegateBuilder!.GetType().GetProperty("PostDelegate")!.GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapPost("", postHandler!);

                    if (haveAuthorization) endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

                    endpoint.WithSummary($"Create {entity.Name}");
                    endpoint.WithDisplayName($"Create {entity.Name}");
                    endpoint.Produces(200, typeof(Result<>).MakeGenericType(responseType));
                    endpoint.Produces(400, typeof(Result));
                    endpoint.Produces(500, typeof(Result));
                }

                if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotUpdateAttribute)))
                {
                    var updateHandler = delegateBuilder!.GetType().GetProperty("UpdateDelegate")!.GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapPut("", updateHandler!);

                    if (haveAuthorization) endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

                    endpoint.WithSummary($"Update {entity.Name}");
                    endpoint.WithDisplayName($"Update {entity.Name}");
                    endpoint.Produces(200, typeof(Result<>).MakeGenericType(responseType));
                    endpoint.Produces(404, typeof(Result));
                    endpoint.Produces(500, typeof(Result));
                }

                if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotDeleteAttribute)))
                {
                    var deleteHandler = delegateBuilder!.GetType().GetProperty("DeleteDelegate")!.GetValue(delegateBuilder) as Delegate;
                    var endpoint = group.MapDelete("{id}", deleteHandler!);

                    if (haveAuthorization) endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());

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

                if (entity.CustomAttributes.Any(c => c.AttributeType == typeof(HasLightListAttribute)))
                {
                    var lightListProperties = entity.GetProperties()
                        .Where(c => c.CustomAttributes.Select(attributeData => attributeData.AttributeType)
                            .Contains(typeof(LightListPropertyAttribute)))
                        .Select(c => c.Name)
                        .ToList();

                    var getLightListDelegate = delegateBuilder!.GetType().GetProperty("GetLightListDelegate")!.GetValue(delegateBuilder) as Delegate;

                    var endpoint = group.MapGet("light-list", getLightListDelegate!);
                    endpoint.WithSummary($"Get light list of {entity.Name}s. Only includes Id and {string.Join(",", lightListProperties)}");

                    if (haveAuthorization) endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());
                }

                var queries = assembly.GetTypes().Where(c => c.BaseType == typeof(Query<>).MakeGenericType(entity));
                foreach (var query in queries)
                {
                    var instance = Activator.CreateInstance(query);
                    var sql = query.GetProperty(nameof(Query<Entity>.Sql))?.GetValue(instance);
                    if (sql is null) continue;

                    // Synchronous read – konfiguracja endpointów nie jest async
                    if ((bool)query.GetProperty(nameof(Query<Entity>.UseSqlFile))?.GetValue(instance)!)
                        sql = File.ReadAllText((string)sql);

                    var queryType = typeBuilder.BuildQueryRequest((sql as string)!, query.Name);
                    var mi = typeof(ApiBuilder).GetMethod("MapGetEndpoint")?.MakeGenericMethod(queryType, query, entity);

                    mi!.Invoke(group, new object[] { group, query.Name, app });
                }

                var fileProperties = entity.GetProperties().Where(c => c.PropertyType == typeof(EntityFile)).ToList();
                foreach (var file in fileProperties)
                {
                    var fileManagementDelegateBuilder = typeof(FileManagementDelegateBuilder<>).MakeGenericType(entity)
                        .GetMethod("Create")
                        ?.MakeGenericMethod(entity)
                        .Invoke(null, new object[] { endpoints.ServiceProvider, file.Name, file.GetCustomAttribute<EntityFileAttribute>()!.Path });

                    var uploadFileDelegate = fileManagementDelegateBuilder!.GetType().GetProperty("UploadFile")!.GetValue(fileManagementDelegateBuilder) as Delegate;
                    var endpoint = group.MapPost($"{file.Name.ToLower()}/{{id}}", uploadFileDelegate!)
                        .DisableAntiforgery();
                    endpoint.WithSummary($"Upload file for {entity.Name}");
                    endpoint.WithDisplayName($"Upload file for {entity.Name}");
                    endpoint.Produces(200, typeof(Result));
                    endpoint.Produces(404, typeof(Result));
                    endpoint.Produces(500, typeof(Result));

                    var deleteFileDelegate = fileManagementDelegateBuilder!.GetType().GetProperty("DeleteFile")!.GetValue(fileManagementDelegateBuilder) as Delegate;
                    endpoint = group.MapDelete($"{file.Name.ToLower()}/{{id}}", deleteFileDelegate!);
                    endpoint.WithSummary($"Delete file for {entity.Name}");
                    endpoint.WithDisplayName($"Delete file for {entity.Name}");
                    endpoint.Produces(200, typeof(Result));
                    endpoint.Produces(404, typeof(Result));
                    endpoint.Produces(500, typeof(Result));

                    var streamFileDelegate = fileManagementDelegateBuilder!.GetType().GetProperty("DownloadFile")!.GetValue(fileManagementDelegateBuilder) as Delegate;
                    endpoint = group.MapGet($"{file.Name.ToLower()}/{{id}}", streamFileDelegate!);
                    endpoint.WithSummary($"Download file for {entity.Name}");
                    endpoint.WithDisplayName($"Download file for {entity.Name}");
                }

                group.WithTags(name);
            }

            using (var scope = app.ApplicationServices.CreateScope())
            {
                var handlers = typeof(CommandBuilder).GetMethod(nameof(CommandBuilder.Build))!.Invoke(null, new object[] { assembly, scope }) as ICollection<IBaseCommandHandler>;
                if (handlers is null) throw new Exception("Could not create command handlers.");

                if (handlers.Count == 0) return;

                foreach (var handler in handlers)
                {
                    var commandType = handler.GetType().GetInterfaces().First().GetGenericArguments()[0];
                    var httpMethod = handler.GetType().GetInterfaces().First().GetGenericArguments()[0]
                        .GetInterfaces()
                        .FirstOrDefault(c => c.GetInterfaces().Any(c => c == typeof(IHttpMethod)));

                    if (httpMethod is null) throw new Exception($"Custom endpoint {commandType.Name} must have HTTP method attribute.");

                    var route = ConvertEndpointNameRegex().Replace(commandType.Name, "$1-$2");
                    if (commandType.GetInterfaces().First().GetGenericArguments()[0]
                            .GetCustomAttribute<RouteAttribute>() is not null)
                    {
                        route = commandType.GetGenericArguments()[0]
                            .GetCustomAttribute<RouteAttribute>()
                            ?.Route;
                    }

                    var customGroup = endpoints.MapGroup(JoinRoute(basePath, "custom"));
                    RouteHandlerBuilder endpoint = null!;

                    if (httpMethod == typeof(IPost))
                    {
                        var delegateMapFeature = typeof(ApiBuilder).GetMethod(nameof(MapPostFeature))?.MakeGenericMethod(
                            commandType, handler.GetType().GetInterfaces().First().GetGenericArguments()[1]);

                        endpoint = delegateMapFeature!.Invoke(null, new object[] { customGroup, handler.GetType(), route! }) as RouteHandlerBuilder
                                   ?? throw new Exception("Could not create endpoint.");
                    }

                    endpoint.Produces(500, typeof(Result));
                    endpoint.WithSummary($"Custom endpoint {commandType.Name}");
                    endpoint.WithDisplayName($"Custom endpoint {commandType.Name}");
                }
            }
        }

        app.UseEndpoints(Configure);
        return app;
    }

    public static RouteHandlerBuilder MapPostFeature<TCommand, TResponse>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand<TResponse> where TResponse : class
    {
        var endpoint = group.MapPost(name, ([FromBody] TCommand request, HttpContext context, IServiceProvider services,
                CancellationToken cancellationToken) =>
        {
            var cp = services.GetRequiredService<IClaimProvider>();
            cp.SetClaims(context.User.Claims.ToList());
            var handler = services.GetRequiredService(handlerType) as ICommandHandler<TCommand, TResponse>;
            
            return handler!.HandleAsync(request, cancellationToken);
        });

        endpoint.Produces(200, typeof(Result<TResponse>));
        return endpoint;
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
            var connectionString = context!.GetConnectionString("DefaultConnection");
            var r = queryHandler.HandleAsync(query, param, connectionString, typeof(TQuery).Assembly, default);
            return r;
        });

        var authorizationPolicies = new List<Type>();
        var haveAuthorization = typeof(TQuery).CustomAttributes.Any(c => c.AttributeType == typeof(SecuredAttribute));
        var authorizationEntityAttribute = typeof(TQuery).GetCustomAttribute<SecuredAttribute>();
        if (authorizationEntityAttribute is not null)
        {
            foreach (var type in authorizationEntityAttribute.EntityTypes)
            {
                if (type.BaseType != typeof(SecuredAttribute))
                    throw new Exception($"AuthorizationEntityAttribute can only have AuthorizationEntityAttribute as EntityTypes. {type.Name}");

                authorizationPolicies.Add(type);
            }
        }

        if (haveAuthorization)
            endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex ConvertEndpointNameRegex();
}
