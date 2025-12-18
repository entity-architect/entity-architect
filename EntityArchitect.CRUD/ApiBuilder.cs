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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.OpenApi.Models;
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
        var enumerable = assembly.ExportedTypes
            .Where(c => c.IsSubclassOf(typeof(Entity)) && !c.IsAbstract)
            .ToList();
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
        
        var normalizedSqlPath = string.IsNullOrEmpty(sqlPath) ? "" : sqlPath.Replace('\\', '/').TrimEnd('/');

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

                if (entity.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationEntityAttribute)))
                {
                    var loginHandler = delegateBuilder!.GetType().GetProperty("Login")!.GetValue(delegateBuilder) as Delegate;
                    var loginEndpoint = group.MapPost("login", loginHandler!);
                    loginEndpoint.WithSummary($"Login {entity.Name}");
                    loginEndpoint.WithDisplayName($"Login {entity.Name}");
                    loginEndpoint.Produces(200, typeof(Result<AuthorizationResponse>));
                    loginEndpoint.Produces(400, typeof(Result));
                    loginEndpoint.Produces(500, typeof(Result));
                    
                    var refreshTokenHandler = delegateBuilder!.GetType().GetProperty("RefreshToken")!.GetValue(delegateBuilder) as Delegate;
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

                var entityPrefix = $"{normalizedSqlPath}/{entity.Name}/";
                foreach (var query in queryFiles.Where(c => c.StartsWith(entityPrefix, StringComparison.OrdinalIgnoreCase)))
                {
                    var sql = File.ReadAllText(query);

                    // Generate endpoint name with snake_case and spaces replaced with dashes
                    var rawEndpointName = Path.GetFileNameWithoutExtension(query);
                    var endpointName = ConvertToSnakeCaseAndReplaceSpaces(rawEndpointName);
                    var queryType = typeBuilder.BuildQueryRequest(sql, endpointName);
                    var mi = typeof(ApiBuilder).GetMethod(nameof(MapGetEndpoint))?.MakeGenericMethod(queryType, entity);

                    var isSingle = false;
                    
                    var relativePath = query.Substring(entityPrefix.Length);
                    var finalEndpointName = ConvertToSnakeCaseAndReplaceSpaces(relativePath.Replace(".sql", ""));
                    mi!.Invoke(group, new object[] { group, finalEndpointName, sql, isSingle, app });
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

            var allEntityNames = enumerable.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var proxyEntity = enumerable.FirstOrDefault();

            if (proxyEntity != null && !string.IsNullOrEmpty(normalizedSqlPath))
            {
                var otherQueries = queryFiles.Where(c =>
                {
                    if (!c.StartsWith(normalizedSqlPath + "/", StringComparison.OrdinalIgnoreCase)) return false;
                    var relative = c.Substring(normalizedSqlPath.Length + 1);
                    if (relative.Contains('/'))
                    {
                        var firstSegment = relative.Split('/')[0];
                        return !allEntityNames.Contains(firstSegment);
                    }
                    return true;
                });

                foreach (var query in otherQueries)
                {
                    var relative = query.Substring(normalizedSqlPath.Length + 1);
                    var parts = relative.Split('/');
                    
                    string groupNameRaw;
                    string relativePathInGroup;

                    if (parts.Length == 1)
                    {
                        // Root file: Queries/Root.sql
                        groupNameRaw = "other";
                        relativePathInGroup = Path.GetFileNameWithoutExtension(parts[0]);
                    }
                    else
                    {
                        // Subfolder: Queries/Group/File.sql
                        groupNameRaw = parts[0];
                        relativePathInGroup = relative.Substring(groupNameRaw.Length).TrimStart('/').Replace(".sql", "");
                    }

                    var groupName = ConvertToSnakeCaseAndReplaceSpaces(groupNameRaw);
                    var group = endpoints.MapGroup(JoinRoute(basePath, groupName)).WithTags(groupName);

                    var sql = File.ReadAllText(query);

                    var rawEndpointName = Path.GetFileNameWithoutExtension(query);
                    var requestEndpointName = ConvertToSnakeCaseAndReplaceSpaces(rawEndpointName);

                    var queryType = typeBuilder.BuildQueryRequest(sql, requestEndpointName);
                    var mi = typeof(ApiBuilder).GetMethod(nameof(MapGetEndpoint))?.MakeGenericMethod(queryType, proxyEntity);

                    var finalEndpointName = ConvertToSnakeCaseAndReplaceSpaces(relativePathInGroup);

                    var isSingle = false;
                    mi!.Invoke(group, new object[] { group, finalEndpointName, sql, isSingle, app });
                }
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
                    var fixedRoute = route.ToLower();
                    if (commandType.GetCustomAttribute<RouteAttribute>() is not null)
                    {
                        route = commandType.GetCustomAttribute<RouteAttribute>()?.Route;
                        group = commandType.GetCustomAttribute<RouteAttribute>()?.Group ?? group;
                        fixedRoute = commandType.GetCustomAttribute<RouteAttribute>()?.FixedRoute ?? fixedRoute;
                    }
                    group = Regex.Replace(group, "([a-z])([A-Z])", "$1-$2").ToLower();
                    
                    var customGroup = endpoints.MapGroup(group).WithTags(group);

                    // Determine HTTP method name
                    var httpMethodName = httpMethod.Name switch
                    {
                        "IPost" => "Post",
                        "IPut" => "Put",
                        "IDelete" => "Delete",
                        "IGet" => "Get",
                        _ => throw new Exception($"Unsupported HTTP method: {httpMethod.Name}")
                    };

                    // Use unified method that handles both response and non-response commands
                    var endpoint = customGroup.MapCommandFeature(handler.GetType(), fixedRoute!, httpMethodName);

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
        where TCommand : ICommand<TResponse> where TResponse : class
    {
        var endpoint = group.MapGet(name, MapCommandEndpoint<TCommand, TResponse>(handlerType, true)).ConfigureEndpoint();
        return AddQueryParametersToOpenApi<TCommand>(endpoint);
    }
    
    public static RouteHandlerBuilder MapPostFeatureSingle<TCommand>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand =>
        group.MapPost(name, MapCommandEndpoint<TCommand>(handlerType)).ConfigureEndpoint();

    public static RouteHandlerBuilder MapPutFeatureSingle<TCommand>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand =>
        group.MapPut(name, MapCommandEndpoint<TCommand>(handlerType)).ConfigureEndpoint();

    public static RouteHandlerBuilder MapDeleteFeatureSingle<TCommand>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand =>
        group.MapDelete(name, MapCommandEndpoint<TCommand>(handlerType, true)).ConfigureEndpoint();

    public static RouteHandlerBuilder MapGetFeatureSingle<TCommand>(this IEndpointRouteBuilder group, Type handlerType, string name)
        where TCommand : ICommand
    {
        var endpoint = group.MapGet(name, (Func<HttpContext, CancellationToken, Task<Result>>)MapCommandEndpoint<TCommand>(handlerType, true)).ConfigureEndpoint();
        return AddQueryParametersToOpenApi<TCommand>(endpoint);
    }

    /// <summary>
    /// Unified method to map any command endpoint (with or without response)
    /// </summary>
    public static RouteHandlerBuilder MapCommandFeature(this IEndpointRouteBuilder group, Type handlerType, string name, string httpMethod)
    {
        Type handlerInterface = null;
        var tmpType = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
        if (tmpType is not null)
        {
            handlerInterface = tmpType;
        }
        else
        {
            tmpType = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
            if (tmpType is not null)
            {
                handlerInterface = tmpType;
            }
        }
        var genericArgs = handlerInterface.GetGenericArguments();

        // Check if this handler has a response type (2 generic args) or not (1 generic arg)
        if (genericArgs.Length == 2)
        {
            // Has response: ICommandHandler<TCommand, TResponse>
            var commandType = genericArgs[0];
            var responseType = genericArgs[1];

            var method = typeof(ApiBuilder).GetMethod($"Map{httpMethod}Feature")
                ?.MakeGenericMethod(commandType, responseType);
            var endpoint = method!.Invoke(null, new object[] { group, handlerType, name }) as RouteHandlerBuilder
                   ?? throw new Exception($"Could not create {httpMethod} endpoint with response.");

            return endpoint.ConfigureEndpoint(handlerType);
        }
        else
        {
            // No response: ICommandHandler<TCommand>
            var commandType = genericArgs[0];

            var method = typeof(ApiBuilder).GetMethod($"Map{httpMethod}FeatureSingle")
                ?.MakeGenericMethod(commandType);
            var endpoint = method!.Invoke(null, new object[] { group, handlerType, name }) as RouteHandlerBuilder
                   ?? throw new Exception($"Could not create {httpMethod} endpoint without response.");

            return endpoint.ConfigureEndpoint(handlerType);
        }
    }

    
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

    /// <summary>
    /// Unified endpoint configuration that automatically detects response type
    /// </summary>
    private static RouteHandlerBuilder ConfigureEndpoint(this RouteHandlerBuilder endpoint, Type handlerType)
    {
        endpoint.Produces(500, typeof(Result));

        // Check if handler has response type
        Type handlerInterface = null;
        var tmpType = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
        if (tmpType is not null)
        {
            handlerInterface = tmpType;
        }
        else
        {
            tmpType = handlerType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
            if (tmpType is not null)
            {
                handlerInterface = tmpType;
            }
        }
        var genericArgs = handlerInterface.GetGenericArguments();

        if (genericArgs.Length == 2)
        {
            // Has response type
            var responseType = genericArgs[1];
            var resultType = typeof(Result<>).MakeGenericType(responseType);
            endpoint.Produces(200, resultType);
        }
        else
        {
            // No response type
            endpoint.Produces(200, typeof(Result));
        }

        return endpoint;
    }

    private static RouteHandlerBuilder AddQueryParametersToOpenApi<TCommand>(RouteHandlerBuilder endpoint)
    {
        var commandType = typeof(TCommand);
        
        // Try to get parameters from constructor first (for record types with constructor parameters)
        var constructor = commandType.GetConstructors()
            .Where(c => c.GetParameters().Length > 0)
            .FirstOrDefault();
        
        List<(string name, Type type, bool isNullable)> parameters = new();
        
        if (constructor != null)
        {
            // Use constructor parameters (for record types like GetMailsCommand(int Page, int ItemCount))
            foreach (var param in constructor.GetParameters())
            {
                var paramType = param.ParameterType;
                var isNullable = Nullable.GetUnderlyingType(paramType) != null || paramType == typeof(string);
                parameters.Add((param.Name!, paramType, isNullable));
            }
        }
        else
        {
            // Use properties (for types generated from SQL queries with properties)
            // Get all properties including inherited ones, but filter out base class properties
            var allProperties = commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var properties = allProperties
                .Where(p => p.DeclaringType != typeof(EntityRequest) && 
                           p.DeclaringType != typeof(object) &&
                           p.CanRead && p.CanWrite)
                .ToList();
            
            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;
                var isNullable = Nullable.GetUnderlyingType(propType) != null || propType == typeof(string);
                parameters.Add((prop.Name, propType, isNullable));
            }
        }
        
        if (parameters.Count == 0)
            return endpoint;

        endpoint.WithOpenApi(op =>
        {
            op.Parameters ??= new List<OpenApiParameter>();
            
            foreach (var (name, type, isNullable) in parameters)
            {
                var openApiParam = new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Query,
                    Required = !isNullable,
                    Description = $"Parameter {name} of type {type.Name}",
                    Schema = GetOpenApiSchema(type)
                };
                
                op.Parameters.Add(openApiParam);
            }
            
            return op;
        });

        return endpoint;
    }

    private static void AddQueryParametersToOpenApiForEndpoint<TParam>(IEndpointConventionBuilder endpoint)
    {
        var type = typeof(TParam);
        
        // Try to get parameters from constructor first (for record types with constructor parameters)
        var constructor = type.GetConstructors()
            .Where(c => c.GetParameters().Length > 0)
            .FirstOrDefault();
        
        List<(string name, Type type, bool isNullable)> parameters = new();
        
        if (constructor != null)
        {
            // Use constructor parameters (for record types like GetMailsCommand(int Page, int ItemCount))
            foreach (var param in constructor.GetParameters())
            {
                var parameterType = param.ParameterType;
                var isNullable = Nullable.GetUnderlyingType(parameterType) != null || parameterType == typeof(string);
                parameters.Add((param.Name!, parameterType, isNullable));
            }
        }
        else
        {
            // Use properties (for types generated from SQL queries with properties)
            // Get only properties declared in this type, not inherited from base classes
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.DeclaringType == type || p.DeclaringType?.IsSubclassOf(typeof(EntityRequest)) == false)
                .ToList();
            
            // If no declared-only properties found, try all properties and filter out base class ones
            if (properties.Count == 0)
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.DeclaringType != typeof(EntityRequest) && p.DeclaringType != typeof(object))
                    .ToList();
            }
            
            foreach (var prop in properties)
            {
                var propertyType = prop.PropertyType;
                var isNullable = Nullable.GetUnderlyingType(propertyType) != null || propertyType == typeof(string);
                parameters.Add((prop.Name, propertyType, isNullable));
            }
        }
        
        if (parameters.Count == 0)
            return;

        endpoint.WithOpenApi(op =>
        {
            op.Parameters ??= new List<OpenApiParameter>();
            
            foreach (var (name, type, isNullable) in parameters)
            {
                var openApiParam = new OpenApiParameter
                {
                    Name = name,
                    In = ParameterLocation.Query,
                    Required = !isNullable,
                    Description = $"Parameter {name} of type {type.Name}",
                    Schema = GetOpenApiSchema(type)
                };
                
                op.Parameters.Add(openApiParam);
            }
            
            return op;
        });
    }

    private static OpenApiSchema GetOpenApiSchema(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType.Name switch
        {
            nameof(Int32) => new OpenApiSchema { Type = "integer", Format = "int32" },
            nameof(Int64) => new OpenApiSchema { Type = "integer", Format = "int64" },
            nameof(String) => new OpenApiSchema { Type = "string" },
            nameof(Boolean) => new OpenApiSchema { Type = "boolean" },
            nameof(Guid) => new OpenApiSchema { Type = "string", Format = "uuid" },
            nameof(DateTime) => new OpenApiSchema { Type = "string", Format = "date-time" },
            nameof(Double) => new OpenApiSchema { Type = "number", Format = "double" },
            nameof(Single) => new OpenApiSchema { Type = "number", Format = "float" },
            nameof(Decimal) => new OpenApiSchema { Type = "number", Format = "decimal" },
            _ => new OpenApiSchema { Type = "string" }
        };
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
            return (Func<HttpContext, CancellationToken, Task<Result<TResponse>>>)
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
            HttpContext context,
            CancellationToken cancellationToken)
            where TCommand : ICommand<TResponse>
            where TResponse : class
        {
            var services = context.RequestServices;

            // Set claims before binding command (in case command constructor uses IClaimProvider)
            var cp = services.GetRequiredService<IClaimProvider>();
            cp.SetClaims(context.User?.Claims?.ToList() ?? new List<Claim>());

            // Custom parameter binding for commands
            var request = BindCommandFromRequest<TCommand, TResponse>(context);

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
            
            // Ensure claims are set before handler execution (in case handler uses IClaimProvider)
          

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
            return new Func<HttpContext, CancellationToken, Task<Result>>(
                async (context, cancellationToken) =>
                {
                    var services = context.RequestServices;

                    // Set claims before binding command (in case command constructor uses IClaimProvider)
                    var cp = services.GetRequiredService<IClaimProvider>();
                    cp.SetClaims(context.User?.Claims?.ToList() ?? new List<System.Security.Claims.Claim>());

                    // Custom parameter binding for commands
                    var command = BindCommandFromRequest<TCommand>(context);

                    // Ensure claims are set before handler execution (in case handler uses IClaimProvider)
                    cp.SetClaims(context.User?.Claims?.ToList() ?? new List<System.Security.Claims.Claim>());

                    var handler = services.GetRequiredService(handlerType) as ICommandHandler<TCommand>;

                    return await handler.HandleAsync(command, cancellationToken);
                });
        }
    }

    private static TCommand BindCommandFromRequest<TCommand, TResponse>(HttpContext context) where TCommand : ICommand<TResponse>
    
    {
        // Set claims before creating command instance (in case constructor uses IClaimProvider)
        var cp = context.RequestServices.GetRequiredService<IClaimProvider>();
        cp.SetClaims(context.User?.Claims?.ToList() ?? new List<Claim>());
        
        var commandType = typeof(TCommand);
        var constructor = commandType.GetConstructors().FirstOrDefault();
        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {commandType.Name}");

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = param.Name!;
            var paramType = param.ParameterType;

            // Try to bind from route data first, then query string
            string? value = null;

            // Check route data
            if (context.Request.RouteValues.TryGetValue(paramName, out var routeValue))
            {
                value = routeValue?.ToString();
            }
            // Check query string
            else if (context.Request.Query.TryGetValue(paramName, out var queryValue))
            {
                value = queryValue.FirstOrDefault();
            }

            if (value != null)
            {
                // Try to convert the value to the parameter type
                try
                {
                    if (paramType == typeof(Guid))
                    {
                        if (Guid.TryParse(value, out var guidValue))
                            args[i] = guidValue;
                        else
                            throw new ArgumentException($"Invalid Guid format: {value}");
                    }
                    else if (paramType == typeof(string))
                    {
                        args[i] = value;
                    }
                    else if (paramType == typeof(int))
                    {
                        if (int.TryParse(value, out var intValue))
                            args[i] = intValue;
                        else
                            throw new ArgumentException($"Invalid integer format: {value}");
                    }
                    else if (paramType == typeof(bool))
                    {
                        if (bool.TryParse(value, out var boolValue))
                            args[i] = boolValue;
                        else
                            throw new ArgumentException($"Invalid boolean format: {value}");
                    }
                    else
                    {
                        // For other types, try to use Convert.ChangeType
                        args[i] = Convert.ChangeType(value, paramType);
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error converting parameter '{paramName}' with value '{value}' to type {paramType.Name}: {ex.Message}");
                }
            }
            else
            {
                // If no value found and parameter is not nullable, throw error
                if (Nullable.GetUnderlyingType(paramType) == null && paramType != typeof(string))
                {
                    throw new ArgumentException($"Required parameter '{paramName}' not found in route or query string");
                }
                else
                {
                    args[i] = paramType.IsValueType ? Activator.CreateInstance(paramType)! : null!;
                }
            }
        }
      
        return (TCommand)constructor.Invoke(args);
    }

    private static TCommand BindCommandFromRequest<TCommand>(HttpContext context) where TCommand : ICommand
    
    {
        // Set claims before creating command instance (in case constructor uses IClaimProvider)
        var cp = context.RequestServices.GetRequiredService<IClaimProvider>();
        cp.SetClaims(context.User?.Claims?.ToList() ?? new List<Claim>());
        
        var commandType = typeof(TCommand);
        var constructor = commandType.GetConstructors().FirstOrDefault();
        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for {commandType.Name}");

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = param.Name!;
            var paramType = param.ParameterType;

            // Try to bind from route data first, then query string
            string? value = null;

            // Check route data
            if (context.Request.RouteValues.TryGetValue(paramName, out var routeValue))
            {
                value = routeValue?.ToString();
            }
            // Check query string
            else if (context.Request.Query.TryGetValue(paramName, out var queryValue))
            {
                value = queryValue.FirstOrDefault();
            }

            if (value != null)
            {
                // Try to convert the value to the parameter type
                try
                {
                    if (paramType == typeof(Guid))
                    {
                        if (Guid.TryParse(value, out var guidValue))
                            args[i] = guidValue;
                        else
                            throw new ArgumentException($"Invalid Guid format: {value}");
                    }
                    else if (paramType == typeof(string))
                    {
                        args[i] = value;
                    }
                    else if (paramType == typeof(int))
                    {
                        if (int.TryParse(value, out var intValue))
                            args[i] = intValue;
                        else
                            throw new ArgumentException($"Invalid integer format: {value}");
                    }
                    else if (paramType == typeof(bool))
                    {
                        if (bool.TryParse(value, out var boolValue))
                            args[i] = boolValue;
                        else
                            throw new ArgumentException($"Invalid boolean format: {value}");
                    }
                    else
                    {
                        // For other types, try to use Convert.ChangeType
                        args[i] = Convert.ChangeType(value, paramType);
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error converting parameter '{paramName}' with value '{value}' to type {paramType.Name}: {ex.Message}");
                }
            }
            else
            {
                // If no value found and parameter is not nullable, throw error
                if (Nullable.GetUnderlyingType(paramType) == null && paramType != typeof(string))
                {
                    throw new ArgumentException($"Required parameter '{paramName}' not found in route or query string");
                }
                else
                {
                    args[i] = paramType.IsValueType ? Activator.CreateInstance(paramType)! : null!;
                }
            }
        }

        return (TCommand)constructor.Invoke(args);
    }

    public static void MapGetEndpoint<TParam, TEntity>(IEndpointRouteBuilder group, string endpointName, string sql, bool isSingle,
        IApplicationBuilder app)
        where TEntity : Entity
        where TParam : class
    {
        QueryHandler<TParam> queryHandler = new();

        var endpoint = group.MapGet(endpointName.ToLower(), ([AsParameters] TParam param, HttpContext httpContext) =>
        {
            var cp = httpContext.RequestServices.GetRequiredService<IClaimProvider>();
            cp.SetClaims(httpContext.User?.Claims?.ToList() ?? new List<Claim>());
            
            var context = httpContext.RequestServices.GetService<IConfiguration>();
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

    /// <summary>
    /// Converts a string to snake_case and replaces spaces with dashes
    /// </summary>
    private static string ConvertToSnakeCaseAndReplaceSpaces(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // First convert PascalCase to snake_case with underscores
        var snakeCase = ConvertEndpointNameRegex().Replace(input, "$1-$2");

        // Then replace spaces with dashes and convert to lowercase
        return snakeCase.Replace(" ", "-").ToLower();
    }

    /// <summary>
    /// Custom model binder for command types that can parse parameters from route data or query string
    /// </summary>
    public class CommandModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var commandType = bindingContext.ModelType;
            var properties = commandType.GetProperties();

            // Try to create the command instance
            var constructor = commandType.GetConstructors().FirstOrDefault();
            if (constructor == null)
                return Task.CompletedTask;

            var parameters = constructor.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name!;
                var paramType = param.ParameterType;

                // Try to bind from route data first, then query string
                string? value = null;

                // Check route data
                if (bindingContext.HttpContext.Request.RouteValues.TryGetValue(paramName, out var routeValue))
                {
                    value = routeValue?.ToString();
                }
                // Check query string
                else if (bindingContext.HttpContext.Request.Query.TryGetValue(paramName, out var queryValue))
                {
                    value = queryValue.FirstOrDefault();
                }

                if (value != null)
                {
                    // Try to convert the value to the parameter type
                    try
                    {
                        if (paramType == typeof(Guid))
                        {
                            if (Guid.TryParse(value, out var guidValue))
                                args[i] = guidValue;
                            else
                                bindingContext.ModelState.AddModelError(paramName, $"Invalid Guid format: {value}");
                        }
                        else if (paramType == typeof(string))
                        {
                            args[i] = value;
                        }
                        else if (paramType == typeof(int))
                        {
                            if (int.TryParse(value, out var intValue))
                                args[i] = intValue;
                            else
                                bindingContext.ModelState.AddModelError(paramName, $"Invalid integer format: {value}");
                        }
                        else if (paramType == typeof(bool))
                        {
                            if (bool.TryParse(value, out var boolValue))
                                args[i] = boolValue;
                            else
                                bindingContext.ModelState.AddModelError(paramName, $"Invalid boolean format: {value}");
                        }
                        else
                        {
                            // For other types, try to use Convert.ChangeType
                            args[i] = Convert.ChangeType(value, paramType);
                        }
                    }
                    catch (Exception ex)
                    {
                        bindingContext.ModelState.AddModelError(paramName, $"Error converting value '{value}' to type {paramType.Name}: {ex.Message}");
                    }
                }
                else
                {
                    // If no value found and parameter is not nullable, add error
                    if (Nullable.GetUnderlyingType(paramType) == null && paramType != typeof(string))
                    {
                        bindingContext.ModelState.AddModelError(paramName, $"Required parameter '{paramName}' not found in route or query string");
                    }
                    else
                    {
                        args[i] = paramType.IsValueType ? Activator.CreateInstance(paramType)! : null!;
                    }
                }
            }

            if (bindingContext.ModelState.IsValid)
            {
                try
                {
                    var model = constructor.Invoke(args);
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
                catch (Exception ex)
                {
                    bindingContext.ModelState.AddModelError("", $"Error creating command instance: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Model binder provider for command types
    /// </summary>
    public class CommandModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Check if the model type implements ICommand interface
            if (typeof(ICommand).IsAssignableFrom(context.Metadata.ModelType))
            {
                return new BinderTypeModelBinder(typeof(CommandModelBinder));
            }

            return null;
        }
    }
}
