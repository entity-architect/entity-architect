using EntityArchitect.CRUD.Designer.Editor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EntityArchitect.CRUD.Designer;

public static class DesignerExtensions
{
    /// <summary>
    /// Enables the Entity Architect Designer endpoints at /__designer.
    /// This provides a visual editor for managing entities and migrations.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    public static IApplicationBuilder UseDesigner(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDesignerEndpoints();
        });
        
        return app;
    }
}
