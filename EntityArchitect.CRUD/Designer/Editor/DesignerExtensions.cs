using EntityArchitect.CRUD.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EntityArchitect.CRUD.Designer.Editor;

public static class DesignerExtensions
{
    /// <summary>
    /// Adds the Entity Architect visual designer if enabled in AddEntityArchitect.
    /// Access at /__designer
    /// </summary>
    public static IApplicationBuilder UseEntityArchitectDesigner(this IApplicationBuilder app)
    {
        if (!DependencyInjection.UseDesignerEnabled)
            return app;
        
        app.UseDesigner();
        
        return app;
    }
    
    /// <summary>
    /// Maps the designer API endpoints. Call after UseEntityArchitectDesigner.
    /// </summary>
    public static IEndpointRouteBuilder MapEntityArchitectDesigner(this IEndpointRouteBuilder app)
    {
        if (!DependencyInjection.UseDesignerEnabled)
            return app;
        
        app.MapDesignerEndpoints();
        
        return app;
    }
}

