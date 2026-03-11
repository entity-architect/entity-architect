using System.Net;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Application;

public class ExceptionMiddleware(RequestDelegate next)
{    
    private readonly RequestDelegate _next = next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(Result.Failure(new Error(HttpStatusCode.InternalServerError, e.Message)));
        }
        finally
        {
            Console.WriteLine($"➡️ Request: {context.Request.Method} {context.Request.Path}\n⬅️ Response: {context.Response.StatusCode}");
        }
    }
}