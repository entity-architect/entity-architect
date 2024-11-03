using System.Reflection;
using System.Text.RegularExpressions;
using EntityArchitect.CRUD.Attributes;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD;

public static partial class DependencyInjection
{
    public static void Main()
    {
    }

    public static WebApplication MapEntityArchitectCrud(this WebApplication app, Assembly assembly)
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

            var delegateBuilder = typeof(DelegateBuilder<,,,>)
                .MakeGenericType(entity, requestPostType, requestUpdateType, responseType)
                .GetMethod("Create")?
                .MakeGenericMethod(entity, requestPostType, requestUpdateType, responseType)
                .Invoke(null, new object[] { app.Services });

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotCreateAttribute)))
            {
                var postHandler =
                    delegateBuilder!.GetType().GetProperty("PostDelegate")!.GetValue(delegateBuilder) as Delegate;
                app.MapPost("/" + name, postHandler!);
            }

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotUpdateAttribute)))
            {
                var updateHandler =
                    delegateBuilder!.GetType().GetProperty("UpdateDelegate")!.GetValue(delegateBuilder) as Delegate;
                app.MapPut("/" + name, updateHandler!);
            }

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotDeleteAttribute)))
            {
                var deleteHandler =
                    delegateBuilder!.GetType().GetProperty("DeleteDelegate")!.GetValue(delegateBuilder) as Delegate;
                app.MapDelete("/" + name + "/{id}", deleteHandler!);
            }

            if (entity.CustomAttributes.All(c => c.AttributeType != typeof(CannotGetByIdAttribute)))
            {
                var getByIdHandler =
                    delegateBuilder!.GetType().GetProperty("GetByIdDelegate")!.GetValue(delegateBuilder) as Delegate;
                app.MapGet("/" + name + "/{id}", getByIdHandler!);
            }
        }

        return app;
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex ConvertEndpointNameRegex();
}