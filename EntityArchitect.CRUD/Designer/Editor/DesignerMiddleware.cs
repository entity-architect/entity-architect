using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace EntityArchitect.CRUD.Designer.Editor;

public static class DesignerMiddleware
{
    public static IApplicationBuilder UseDesigner(this IApplicationBuilder app, string basePath = "/__designer")
    {
        var assembly = typeof(DesignerMiddleware).Assembly;
        
        // Serve embedded static files
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new EmbeddedFileProvider(assembly, "EntityArchitect.CRUD.Designer.Editor.wwwroot"),
            RequestPath = basePath
        });
        
        // Serve index.html for SPA routing
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(basePath) && 
                !context.Request.Path.Value!.Contains('.') &&
                !context.Request.Path.Value.StartsWith($"{basePath}/model") &&
                !context.Request.Path.Value.StartsWith($"{basePath}/migrations") &&
                !context.Request.Path.Value.StartsWith($"{basePath}/schema"))
            {
                var indexHtml = GetEmbeddedResource(assembly, "EntityArchitect.CRUD.Designer.Editor.wwwroot.index.html");
                if (indexHtml != null)
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(indexHtml);
                    return;
                }
            }
            
            await next();
        });
        
        return app;
    }
    
    private static string? GetEmbeddedResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;
        
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

