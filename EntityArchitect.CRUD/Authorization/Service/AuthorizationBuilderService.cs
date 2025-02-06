using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using EntityArchitect.CRUD.Authorization.Attributes;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using EntityArchitect.CRUD.Authorization.Responses;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.Extensions.Configuration;


namespace EntityArchitect.CRUD.Authorization.Service;

public class AuthorizationBuilderService : IAuthorizationBuilderService
{
    public AuthorizationResponse CreateAuthorizationToken<TAuthorizationEntity>(TAuthorizationEntity entity) where TAuthorizationEntity : Entity
    {
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
        var configuration = builder.Build();
        
        var authorizationKey = Encoding.ASCII.GetBytes(configuration["Jwt:AuthorizationKey"]!);
        var refreshKey = Encoding.ASCII.GetBytes(configuration["Jwt:RefreshKey"]!);
        var claims = new List<Claim>
        {
            new("id", entity.Id.Value.ToString()),
            new(ClaimTypes.Role, entity.GetType().Name)
        };
        
        var claimsProperties = entity.GetType().GetProperties().Where(p => p.CustomAttributes.Any(c => c.AttributeType == typeof(AuthorizationClaimAttribute))).ToList();
        claims.AddRange(claimsProperties.Select(property => new Claim(property.Name, property.GetValue(entity)?.ToString() ?? string.Empty)));
        
        var authorizationTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddSeconds(configuration.GetValue<int>("Jwt:AuthorizationExpiration")),
            Audience = configuration["Jwt:Audience"]!,
            Issuer = configuration["Jwt:Issuer"]!,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(authorizationKey),
                SecurityAlgorithms.HmacSha512Signature)
        };
        var authorizationTokenHandler = new JwtSecurityTokenHandler();
        var authorizationToken = authorizationTokenHandler.CreateToken(authorizationTokenDescriptor);
        
                
        var refreshTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddSeconds(configuration.GetValue<int>("Jwt:RefreshExpiration")),
            Audience = configuration["Jwt:Audience"]!,
            Issuer = configuration["Jwt:Issuer"]!,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(refreshKey),
                SecurityAlgorithms.HmacSha512Signature)
        };
        var refreshTokenHandler = new JwtSecurityTokenHandler();
        var refreshToken = refreshTokenHandler.CreateToken(refreshTokenDescriptor);

        
        return new AuthorizationResponse()
        {
            AuthorizationToken = refreshTokenHandler.WriteToken(authorizationToken),
            AuthorizationTokenExpiration = DateTime.Now.AddSeconds(configuration.GetValue<int>("Jwt:AuthorizationExpiration")),
            RefreshToken = refreshTokenHandler.WriteToken(refreshToken),
            RefreshTokenExpiration = DateTime.Now.AddSeconds(configuration.GetValue<int>("Jwt:RefreshExpiration"))
        };
    }
}