using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Services;
using Microsoft.AspNetCore.Authorization;

namespace EntityArchitect.CRUD.Middlewares;


public class ClaimProviderMiddleware(IClaimProvider claimProvider) : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();
        
        if (endpoint is null)
        {
            return next(context);
        }
        else
        {
            var hasAuthorizations = endpoint.Metadata.Any(c => c.GetType() == typeof(AuthorizeAttribute));
            var forAnonymous = context.GetEndpoint()!.Metadata.Any(c => c.GetType() == typeof(AllowAnonymousAttribute));
            if (!hasAuthorizations || forAnonymous)
            {
                return next(context);
            }

            claimProvider.SetClaims(context.User.Claims.ToList());
            return next(context);
        }
    }
}