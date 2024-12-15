using System.Reflection;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Authorization.Service;
using EntityArchitect.Entities.Entities;
using Microsoft.AspNetCore.Authorization;

namespace EntityArchitect.CRUD.Authorization;

public static class AuthorizationBuilder
{
    public static IServiceCollection BuildEntityArchitectAuthorization(this IServiceCollection services, Assembly assembly)
    { 
        var userEntities = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity) && c.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationEntityAttribute))).ToList();
        if(userEntities.Count == 0) throw new Exception("No authorization entities found.");
        services.AddAuthorization(options =>
        {
            foreach (var userEntity in userEntities)
            {
                var name = userEntity.CustomAttributes.First(c => c.AttributeType == typeof(AuthorizationEntityAttribute)).ConstructorArguments[0].Value ?? userEntity.Name;
                options.AddPolicy(name.ToString()!, policy => policy.RequireRole(name.ToString()!));
            }
        });

        services.AddScoped<IAuthorization, Service.Authorization>();
        
        return services;
    }
    
    public static IApplicationBuilder AddEntityArchitectAuthorization(this IApplicationBuilder app)
    {
        var sp = app.ApplicationServices;
        var auth = sp.GetService<IAuthorization>(); //TODO
        if(auth is null) throw new Exception("IAuthorization service not found");
        app.UseMiddleware<AuthorizationMiddleware>();
        return app;
    }
    
    
}