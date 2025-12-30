using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.Authorization.Service;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EntityArchitect.CRUD.Authorization;

public static class AuthorizationBuilder
{
    private static readonly Dictionary<string, string[]> RegisteredPolicies = new();
    
    public static IServiceCollection BuildEntityArchitectAuthorization(this IServiceCollection services, Assembly assembly)
    { 
        services.AddTransient<IAuthorizationBuilderService, AuthorizationBuilderService>();
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();
        var key = Encoding.UTF8.GetBytes(configuration.GetValue<string>("Jwt:AuthorizationKey"));
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
        
        // Collect all SecuredAttribute usages to build combined policies
        var entitiesWithSecured = assembly.ExportedTypes
            .Where(c => c.BaseType == typeof(Entity) && c.GetCustomAttribute<SecuredAttribute>() != null)
            .ToList();
        
        foreach (var entity in entitiesWithSecured)
        {
            var securedAttr = entity.GetCustomAttribute<SecuredAttribute>()!;
            var roleNames = securedAttr.SecuredByTypes.Select(t => t.Name).OrderBy(n => n).ToArray();
            var policyName = GetPolicyName(roleNames);
            if (!RegisteredPolicies.ContainsKey(policyName))
            {
                RegisteredPolicies[policyName] = roleNames;
            }
        }
        
        services.AddAuthorization(options =>
        {
            // Add single-role policies for each authorization entity
            foreach (var userEntity in userEntities)
            {
                options.AddPolicy(userEntity.Name, policy => policy.RequireRole(userEntity.Name));
            }
            
            // Add combined policies (OR logic - any of the roles)
            foreach (var kvp in RegisteredPolicies)
            {
                if (kvp.Value.Length > 1)
                {
                    options.AddPolicy(kvp.Key, policy => policy.RequireRole(kvp.Value));
                }
            }
        });
        
        return services;
    }
    
    public static string GetPolicyName(params Type[] types)
    {
        var names = types.Select(t => t.Name).OrderBy(n => n).ToArray();
        return GetPolicyName(names);
    }
    
    public static string GetPolicyName(string[] roleNames)
    {
        if (roleNames.Length == 1) return roleNames[0];
        return string.Join("Or", roleNames);
    }
}