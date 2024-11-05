using System.Reflection;
using System.Text.RegularExpressions;
using EntityArchitect.CRUD.Attributes;
using EntityArchitect.CRUD.TypeBuilders;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD;

public static partial class DependencyInjection
{
    public static void Main()
    {
    }

    public static WebApplication MapEntityArchitectCrud(this WebApplication app, Assembly assembly,string basePath = "")
    {
        var enumerable = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        var typeBuilder = new TypeBuilder();
        foreach (var entity in enumerable)
        {
            var result = ConvertEndpointNameRegex().Replace(entity.Name, "$1-$2");
            var name = result.ToLower();

            var requestPostType = typeBuilder.BuildCreateRequestFromEntity(entity);
            var requestUpdateType = typeBuilder.BuildUpdateRequestFromEntity(entity);
            var responseType = typeBuilder.BuildResponseFromEntity(entity);
            var lightListResponseType = typeBuilder.BuildLightListProperty(entity);
            
            var group = app.MapGroup(Path.Combine(basePath, name));
            
            var delegateBuilder = typeof(DelegateBuilder<,,,,>)
                .MakeGenericType(entity, requestPostType, requestUpdateType, responseType, lightListResponseType)
                .GetMethod("Create")?
                .MakeGenericMethod(entity, requestPostType, requestUpdateType, responseType, lightListResponseType)
                .Invoke(null, new object[] { app.Services });

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotCreateAttribute)))
            {
                var postHandler =
                    delegateBuilder!.GetType().GetProperty("PostDelegate")!.GetValue(delegateBuilder) as Delegate;
                group.MapPost("",postHandler!);
            }

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotUpdateAttribute)))
            {
                var updateHandler =
                    delegateBuilder!.GetType().GetProperty("UpdateDelegate")!.GetValue(delegateBuilder) as Delegate;
                group.MapPut("", updateHandler!);
            }

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotDeleteAttribute)))
            {
                var deleteHandler =
                    delegateBuilder!.GetType().GetProperty("DeleteDelegate")!.GetValue(delegateBuilder) as Delegate;
                group.MapDelete("{id}", deleteHandler!);
            }

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotGetByIdAttribute)))
            {
                var getByIdHandler =
                    delegateBuilder!.GetType().GetProperty("GetByIdDelegate")!.GetValue(delegateBuilder) as Delegate;
                group.MapGet("{id}", getByIdHandler!);
            }
            
            if (entity.CustomAttributes.Any(c => c.AttributeType == typeof(HasLightListAttribute)))
            {
                var getLightListDelegate =
                    delegateBuilder!.GetType().GetProperty("GetLightListDelegate")!.GetValue(delegateBuilder) as Delegate;
                group.MapGet("light-list", getLightListDelegate!);
            } 
            
            if (entity.CustomAttributes.Any(c => c.AttributeType== typeof(GetListPaginatedAttribute)))
            {
                var getLightListDelegate =
                    delegateBuilder!.GetType().GetProperty("GetListDelegate")!.GetValue(delegateBuilder) as Delegate;
                group.MapGet("list/{page}", getLightListDelegate!);
            }
            group.WithTags(name);
        }
        
        return app;
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex ConvertEndpointNameRegex();
}