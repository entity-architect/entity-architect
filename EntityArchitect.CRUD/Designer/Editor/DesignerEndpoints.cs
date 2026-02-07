using System.Net;
using EntityArchitect.CRUD.Designer.ApplicationModels;
using EntityArchitect.CRUD.Results.Abstracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace EntityArchitect.CRUD.Designer.Editor;

public static class DesignerEndpoints
{
    private const string MigrationsFolder = "ApplicationMigrations";
    
    public static void MapDesignerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/__designer");
        
        // Get current model
        group.MapGet("/model", async () =>
        {
            try
            {
                if (!Directory.Exists(MigrationsFolder))
                    return Results.Ok(new ApplicationModel());
                
                var lastMigrationFile = Directory.GetFiles(MigrationsFolder).MaxBy(f => f);
                if (lastMigrationFile is null)
                    return Results.Ok(new ApplicationModel());
                
                var json = await File.ReadAllTextAsync(lastMigrationFile);
                var model = JsonConvert.DeserializeObject<ApplicationModel>(json);
                
                return Results.Ok(model ?? new ApplicationModel());
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
        
        // Save model (creates new migration)
        group.MapPost("/model", async (ApplicationModel model) =>
        {
            try
            {
                if (!Directory.Exists(MigrationsFolder))
                    Directory.CreateDirectory(MigrationsFolder);
                
                var fileName = Path.Combine(MigrationsFolder, $"Migration{DateTime.Now:yyyyMMddHHmmss}.json");
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                await File.WriteAllTextAsync(fileName, json);
                
                return Results.Ok(new { success = true, fileName });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
        
        // Get all migrations
        group.MapGet("/migrations", () =>
        {
            try
            {
                if (!Directory.Exists(MigrationsFolder))
                    return Results.Ok(Array.Empty<string>());
                
                var files = Directory.GetFiles(MigrationsFolder)
                    .Select(Path.GetFileName)
                    .OrderByDescending(f => f)
                    .ToList();
                
                return Results.Ok(files);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
        
        // Get specific migration
        group.MapGet("/migrations/{fileName}", async (string fileName) =>
        {
            try
            {
                var filePath = Path.Combine(MigrationsFolder, fileName);
                if (!File.Exists(filePath))
                    return Results.NotFound();
                
                var json = await File.ReadAllTextAsync(filePath);
                var model = JsonConvert.DeserializeObject<ApplicationModel>(json);
                
                return Results.Ok(model);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
        
        // Get available attributes info
        group.MapGet("/schema", () =>
        {
            var schema = new
            {
                EntityAttributes = new[]
                {
                    new { Name = "CannotCreate", Type = "bool", Description = "Disable POST endpoint" },
                    new { Name = "CannotUpdate", Type = "bool", Description = "Disable PUT endpoint" },
                    new { Name = "CannotDelete", Type = "bool", Description = "Disable DELETE endpoint" },
                    new { Name = "CannotGetById", Type = "bool", Description = "Disable GET by ID endpoint" },
                    new { Name = "HasLightList", Type = "bool", Description = "Enable light list endpoint" },
                    new { Name = "GetListPaginated", Type = "bool", Description = "Enable pagination" },
                    new { Name = "PaginatedItemCount", Type = "int?", Description = "Items per page (default 10)" }
                },
                PropertyAttributes = new[]
                {
                    new { Name = "IgnorePostRequest", Type = "bool", Description = "Ignore in POST request" },
                    new { Name = "IgnorePutRequest", Type = "bool", Description = "Ignore in PUT request" },
                    new { Name = "IncludeInGet", Type = "bool", Description = "Include in GET response" },
                    new { Name = "IncludingDeep", Type = "int?", Description = "Depth of inclusion" },
                    new { Name = "LightListProperty", Type = "bool", Description = "Include in light list" }
                },
                PropertyTypes = new[] { "string", "int", "long", "bool", "datetime", "double", "decimal", "guid" },
                RelationTypes = new[] { "OneToOne", "OneToMany", "ManyToOne" }
            };
            
            return Results.Ok(schema);
        });
    }
}

