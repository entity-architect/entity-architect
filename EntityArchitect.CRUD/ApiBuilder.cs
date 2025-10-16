using System.Reflection;
using System.Text.RegularExpressions;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using EntityArchitect.CRUD.Application;
using EntityArchitect.CRUD.Attributes.CrudAttributes;
using EntityArchitect.CRUD.Authorization;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Authorization.Responses;
using EntityArchitect.CRUD.Authorization.Service;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Feature;
using EntityArchitect.CRUD.Feature.Methods;
using EntityArchitect.CRUD.Files;
using EntityArchitect.CRUD.Helpers;
using EntityArchitect.CRUD.Queries;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using EntityArchitect.CRUD.TypeBuilders;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = EntityArchitect.CRUD.Feature.RouteAttribute;

namespace EntityArchitect.CRUD;

public static partial class ApiBuilder
{
    public static void Main()
    {
    }

    private static string JoinRoute(params string[] segments) =>
        "/" + string.Join('/', segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim('/')));

    public static IApplicationBuilder MapEntityArchitectCrud(this IApplicationBuilder app, Assembly assembly,
        string basePath = "", string sqlPath = "")
    {
        var enumerable = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        var typeBuilder = new TypeBuilder();

        app.UseRouting();
        app.UseMiddleware<ExceptionMiddleware>();

        var auth = app.ApplicationServices.GetService(typeof(IAuthorizationBuilderService));
        if (auth is not null)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<AuthorizationMiddleware>();
        }

        app.UseAntiforgery();
        
        var queryFiles = string.IsNullOrEmpty(sqlPath)
            ? Array.Empty<string>()
            : Directory.GetFiles(sqlPath, "*.sql", SearchOption.AllDirectories)
                .Select(f => f.Replace('\\', '/'))
                .ToArray();

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
                    foreach (var type in authorizationEntityAttribute.SecuredByTypes)
                    {
                        if (type.CustomAttributes.All(c => c.AttributeType != typeof(AuthorizationEntityAttribute)))
                            throw new Exception($"AuthorizationEntityAttribute can only have AuthorizationEntityAttribute as EntityTypes. {type.Name}");
                        authorizationPolicies.Add(type);
                    }
                }

                var requestPostType = typeBuilder.BuildCreateRequestFromEntity(entity);
                var requestUpdateType = typeBuilder.BuildUpdateRequestFromEntity(entity);
                var responseType = typeBuilder.BuildResponseFromEntity(entity);

                var group = endpoints.MapGroup(JoinRoute(basePath, name)).WithTags(entity.Name.ToLower());

                var delegateBuilder = typeof(DelegateBuilder<,,,>).MakeGenericType(entity, requestPostType, requestUpdateType, responseType)
                    .GetMethod("Create")
                    ?.MakeGenericMethod(entity, requestPostType, requestUpdateType, responseType)
                    .Invoke(null, new object[] { endpoints.ServiceProvider });

                if (entity.CustomAttributes.Any(c => c.AttributeType != typeof(AuthorizationEntityAttribute)))
                {
                    var loginHandler = delegateBuilder!.GetType().GetProperty("Login")!.GetValue(delegateBuilder) as Delegate;
                    var loginEndpoint = group.MapPost("login", loginHandler!);
                    loginEndpoint.WithSummary($"Login {entity.Name}");
                    loginEndpoint.WithDisplayName($"Login {entity.Name}");
                    loginEndpoint.Produces(200, typeof(Result<AuthorizationResponse>));
                    loginEndpoint.Produces(400, typeof(Result));
                    loginEndpoint.Produces(500, typeof(Result));
                    
                    var refreshTokenHandler = delegateBuilder!.GetType().GetProperty("Login")!.GetValue(delegateBuilder) as Delegate;
                    var refreshTokenEndpoint = group.MapPost("refresh", refreshTokenHandler!);
                    refreshTokenEndpoint.WithSummary($"Refresh Token {entity.Name}");
                    refreshTokenEndpoint.WithDisplayName($"Refresh Token {entity.Name}");
                    refreshTokenEndpoint.Produces(200, typeof(Result<AuthorizationResponse>));
                    refreshTokenEndpoint.Produces(400, typeof(Result));
                    refreshTokenEndpoint.Produces(500, typeof(Result));
                }
                

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
                    var deleteHandler = delegateBuilder!.GetType().GetProperty(nameof(DelegateBuilder<Entity, Entity, Entity, Response>.DeleteDelegate))!.GetValue(delegateBuilder) as Delegate;
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

                
                foreach (var query in queryFiles.Where(c => 
                             string.Equals(c.Split("/")[0], sqlPath,     StringComparison.CurrentCultureIgnoreCase) &&
                             string.Equals(c.Split("/")[1], entity.Name, StringComparison.CurrentCultureIgnoreCase)))
                {
                    var sql = File.ReadAllText(query);

                    var endpointName = query.Split("/").Last().Replace(".sql", "").ToLower();
                    var queryType = typeBuilder.BuildQueryRequest(sql, endpointName);
                    var mi = typeof(ApiBuilder).GetMethod(nameof(MapGetEndpoint))?.MakeGenericMethod(queryType, entity);

                    var isSingle = false;
                    mi!.Invoke(group, new object[] { group, query.Replace(sqlPath + "/" + entity.Name + "/", "").Replace(".sql", ""), sql ,isSingle, app });
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

                group.WithTags(name.ToLower());
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
                    var group = "custom";
                    if (commandType.GetCustomAttribute<RouteAttribute>() is not null)
                    {
                        route = commandType.GetCustomAttribute<RouteAttribute>()?.Route;
                        group = commandType.GetCustomAttribute<RouteAttribute>()?.Group ?? group;
                    }
                    
                    var customGroup = endpoints.MapGroup(group).WithTags(group);
                    RouteHandlerBuilder endpoint = null!;

                    if (httpMethod == typeof(IPost))
                    {
                        var delegateMapFeature = typeof(ApiBuilder).GetMethod(nameof(MapPostFeature))?.MakeGenericMethod(
                            commandType, handler.GetType().GetInterfaces().First().GetGenericArguments()[1]);

                        endpoint = delegateMapFeature!.Invoke(null, new object[] { customGroup, handler.GetType(), route! }) as RouteHandlerBuilder
                                   ?? throw new Exception("Could not create endpoint.");
                    }
                    
                    if (httpMethod == typeof(IPut))
                    {
                        var delegateMapFeature = typeof(ApiBuilder).GetMethod(nameof(MapPutFeature))?.MakeGenericMethod(
                            commandType, handler.GetType().GetInterfaces().First().GetGenericArguments()[1]);

                        endpoint = delegateMapFeature!.Invoke(null, new object[] { customGroup, handler.GetType(), route! }) as RouteHandlerBuilder
                                   ?? throw new Exception("Could not create endpoint.");
                    }
                    
                    if (httpMethod == typeof(IDelete))
                    {
                        var delegateMapFeature = typeof(ApiBuilder).GetMethod(nameof(MapDeleteFeature))?.MakeGenericMethod(
                            commandType, handler.GetType().GetInterfaces().First().GetGenericArguments()[1]);

                        endpoint = delegateMapFeature!.Invoke(null, new object[] { customGroup, handler.GetType(), route! }) as RouteHandlerBuilder
                                   ?? throw new Exception("Could not create endpoint.");
                    }
                    
                    if (httpMethod == typeof(IGet))
                    {
                        var delegateMapFeature = typeof(ApiBuilder).GetMethod(nameof(MapGetFeature))?.MakeGenericMethod(
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
        var endpointDataSource = app.ApplicationServices.GetRequiredService<EndpointDataSource>();

        var authorizationEntities = enumerable
            .Where(c => c.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationEntityAttribute)))
            .ToList();
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Zbuduj aktualną listę map
            var current = endpointDataSource.Endpoints.Select(e =>
            {
                var hash = e.CreateEndpointHash(); // lub Twoje CreateEndpointHash(e)
                var path = (e.Metadata.FirstOrDefault(m => m is IRouteDiagnosticsMetadata) as IRouteDiagnosticsMetadata)?.Route 
                           ?? "UNKNOWN";
                var httpMethod = e.Metadata.FirstOrDefault(m => m is IHttpMethodMetadata) is IHttpMethodMetadata m
                    ? string.Join(",", m.HttpMethods)
                    : "UNKNOWN";

                return new EndpointMap
                {
                    Hash = hash,
                    Path = path,
                    HttpMethod = httpMethod,
                    Accesses = authorizationEntities.Select(c => new Access()
                    {
                        User = c.Name,
                        Allowed = true
                    }).ToList()
                };
            }).ToList();

            var existingByHash = db.Set<EndpointMap>()
                .AsQueryable()
                .ToDictionary(x => x.Hash, x => x);

            foreach (var map in current)
            {
                if (existingByHash.TryGetValue(map.Hash, out var existing))
                {
                    existing.Path = map.Path;
                    existing.HttpMethod = map.HttpMethod;
                    existingByHash.Remove(map.Hash);
                }
                else
                {
                    db.Set<EndpointMap>().Add(map);
                }
            }

            if (existingByHash.Count > 0)
            {
                db.Set<EndpointMap>().RemoveRange(existingByHash.Values);
            }

            db.SaveChanges();
        }
        
        return app;
    }

    private static string CreateEndpointHash(this Endpoint c)
    {
        var s = (c.Metadata.First(m => m is IRouteDiagnosticsMetadata) as IRouteDiagnosticsMetadata)?.Route.Replace("/",
                    "") +
                (c.Metadata.First(m => m is IHttpMethodMetadata) as IHttpMethodMetadata)?.HttpMethods[0];
        return Convert.ToHexString(MD5.HashData(System.Text.Encoding.UTF8.GetBytes(s)));
    }

    public static RouteHandlerBuilder MapPostFeature<TCommand, TResponse>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand<TResponse> where TResponse : class =>
        group.MapPost(name, MapCommandEndpoint<TCommand, TResponse>(handlerType)).ConfigureEndpoint<TResponse>();

    public static RouteHandlerBuilder MapPutFeature<TCommand, TResponse>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand<TResponse> where TResponse : class =>
        group.MapPut(name, MapCommandEndpoint<TCommand, TResponse>(handlerType)).ConfigureEndpoint<TResponse>();

    public static RouteHandlerBuilder MapDeleteFeature<TCommand, TResponse>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand<TResponse> where TResponse : class =>
        group.MapDelete(name, MapCommandEndpoint<TCommand, TResponse>(handlerType, true)).ConfigureEndpoint();

    public static RouteHandlerBuilder MapGetFeature<TCommand, TResponse>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand<TResponse> where TResponse : class =>
        group.MapGet(name, MapCommandEndpoint<TCommand, TResponse>(handlerType, true)).ConfigureEndpoint();
    
    
    private static RouteHandlerBuilder ConfigureEndpoint<TResponse>(this RouteHandlerBuilder endpoint) where TResponse : class
    {
        endpoint.Produces(500, typeof(Result));
        endpoint.Produces(200, typeof(Result<TResponse>));
        return endpoint;
    }
    private static RouteHandlerBuilder ConfigureEndpoint(this RouteHandlerBuilder endpoint)
    {
        endpoint.Produces(500, typeof(Result));
        endpoint.Produces(200, typeof(Result));
        return endpoint;
    }

    private static Delegate MapCommandEndpoint<TCommand, TResponse>(Type handlerType, bool fromQuery = false)
        where TCommand : ICommand<TResponse> where TResponse : class
    {
        if (!fromQuery)
        {
            return (Func<TCommand, HttpContext, CancellationToken, Task<Result<TResponse>>>)
                HandleFromBody<TCommand, TResponse>;
        }
        else
        {
            return (Func<TCommand, HttpContext, CancellationToken, Task<Result<TResponse>>>)
                HandleFromQuery<TCommand, TResponse>;
        }


        async Task<Result<TResponse>> HandleFromBody<TCommand, TResponse>(
            [FromBody] TCommand request,
            HttpContext context,
            CancellationToken cancellationToken)
            where TCommand : ICommand<TResponse>
            where TResponse : class
        {
            var services = context.RequestServices;
            
            var validator = typeof(TCommand).Assembly.GetTypes().FirstOrDefault(c => c.BaseType == typeof(AbstractValidator<>).MakeGenericType(typeof(TCommand)));
            if (validator is not null)
            {
                if (Activator.CreateInstance(validator) is AbstractValidator<TCommand> v)
                {
                    var validationResult = await v.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                    { 
                        return Result.Failure<TResponse>(validationResult.Errors.Select(c => new Error(HttpStatusCode.BadRequest, c.ErrorMessage)).ToList());
                    }
                }
            }
            
            var cp = services.GetRequiredService<IClaimProvider>();
            cp.SetClaims(context.User?.Claims?.ToList() ?? new List<Claim>());

            var handlerObj = services.GetRequiredService(handlerType);
            var handler = (ICommandHandler<TCommand, TResponse>)handlerObj;

            return await handler.HandleAsync(request, cancellationToken);
        }

        async Task<Result<TResponse>> HandleFromQuery<TCommand, TResponse>(
            [AsParameters] TCommand request,
            HttpContext context,
            CancellationToken cancellationToken)
            where TCommand : ICommand<TResponse>
            where TResponse : class
        {
            var services = context.RequestServices;
            var validator = typeof(TCommand).Assembly.GetTypes().FirstOrDefault(c => c.BaseType == typeof(AbstractValidator<>).MakeGenericType(typeof(TCommand)));
            if (validator is not null)
            {
                if (Activator.CreateInstance(validator) is AbstractValidator<TCommand> v)
                {
                    var validationResult = await v.ValidateAsync(request, cancellationToken);
                    if (!validationResult.IsValid)
                    { 
                        return Result.Failure<TResponse>(validationResult.Errors.Select(c => new Error(HttpStatusCode.BadRequest, c.ErrorMessage)).ToList());
                    }
                }
            }
            var cp = services.GetRequiredService<IClaimProvider>();
            cp.SetClaims(context.User?.Claims?.ToList() ?? new List<Claim>());

            var handlerObj = services.GetRequiredService(handlerType);
            var handler = (ICommandHandler<TCommand, TResponse>)handlerObj;

            return await handler.HandleAsync(request, cancellationToken);
        }
    }



    private static Delegate MapCommandEndpoint<TCommand>(Type handlerType, bool fromQuery = false)
        where TCommand : ICommand
    {
        if (!fromQuery)
            return new Func<TCommand, HttpContext, CancellationToken, Task<Result>>(
                ([FromBody] request, context, cancellationToken) =>
                {
                    var services = context.RequestServices;

                    var cp = services.GetRequiredService<IClaimProvider>();
                    cp.SetClaims(context.User?.Claims?.ToList() ?? new List<System.Security.Claims.Claim>());
                    var handler = services.GetRequiredService(handlerType) as ICommandHandler<TCommand>;

                    return handler.HandleAsync(request, cancellationToken);
                });
        else
        {
            return new Func<TCommand, HttpContext, CancellationToken, Task<Result>>(
                ([FromQuery] request, context, cancellationToken) =>
                {
                    var services = context.RequestServices;

                    var cp = services.GetRequiredService<IClaimProvider>();
                    cp.SetClaims(context.User?.Claims?.ToList() ?? new List<System.Security.Claims.Claim>());
                    var handler = services.GetRequiredService(handlerType) as ICommandHandler<TCommand>;

                    return handler.HandleAsync(request, cancellationToken);
                });
        }
    }

    public static void MapGetEndpoint<TParam, TEntity>(IEndpointRouteBuilder group, string endpointName, string sql, bool isSingle,
        IApplicationBuilder app)
        where TEntity : Entity
        where TParam : class
    {
        QueryHandler<TParam> queryHandler = new();

        var endpoint = group.MapGet(endpointName.ToLower(), ([AsParameters] TParam param) =>
        {
            var context = app.ApplicationServices.GetService<IConfiguration>();
            var connectionString = context!.GetConnectionString("DefaultConnection");
            var r = queryHandler.HandleAsync(sql, endpointName, param, connectionString, typeof(TEntity).Assembly, isSingle);
            return r;
        });

        //var authorizationPolicies = new List<Type>();
        //var haveAuthorization = typeof(TQuery).CustomAttributes.Any(c => c.AttributeType == typeof(SecuredAttribute));
        //var authorizationEntityAttribute = typeof(TQuery).GetCustomAttribute<SecuredAttribute>();
        //if (authorizationEntityAttribute is not null)
        //{
        //    foreach (var type in authorizationEntityAttribute.EntityTypes)
        //    {
        //        if (type.BaseType != typeof(SecuredAttribute))
        //            throw new Exception($"AuthorizationEntityAttribute can only have AuthorizationEntityAttribute as EntityTypes. {type.Name}");

        //        authorizationPolicies.Add(type);
        //    }
        //}

        //if (haveAuthorization)
        //    endpoint.RequireAuthorization(authorizationPolicies.Select(c => c.Name).ToArray());
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex ConvertEndpointNameRegex();
}
