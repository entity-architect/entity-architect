using System.Reflection;
using System.Text;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Authorization.Service;
using EntityArchitect.Entities.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EntityArchitect.CRUD.Authorization;

public static class AuthorizationBuilder
{
    public static IServiceCollection BuildEntityArchitectAuthorization(this IServiceCollection services, Assembly assembly)
    { 
        services.AddTransient<IAuthorizationBuilderService, AuthorizationBuilderService>();
        var configuration =services.BuildServiceProvider().GetService<IConfiguration>();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:AuthorizationKey"]);
        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
            }
        );
        var userEntities = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity) && c.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationEntityAttribute))).ToList();
        if(userEntities.Count == 0) throw new Exception("No authorization entities found.");
        services.AddAuthorization(options =>
        {
            foreach (var userEntity in userEntities)
            {
                options.AddPolicy(userEntity.Name, policy => policy.RequireRole(userEntity.Name));
            }
        });
        
        return services;
    }
}