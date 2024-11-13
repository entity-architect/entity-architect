using System.Reflection;
using EntityArchitect.Entities.Entities;

namespace EntityArchitect.CRUD.Actions;

public static class ActionsBuilder
{
    public static WebApplicationBuilder UseActions(this WebApplicationBuilder builder, Assembly assembly)
    {
        var entities = assembly.GetTypes().Where(c => c.BaseType == typeof(Entity)).ToList();

        foreach (var entity in entities)
        {
            var actions =
                assembly.GetTypes()
                    .Where(c => c.BaseType ==
                                typeof(EndpointAction<>).MakeGenericType(entity))
                    .ToList();

            actions.ForEach(action =>
                builder.Services.AddScoped(action));
        }

        return builder;
    }

    public static WebApplicationBuilder UseActions(this WebApplicationBuilder builder)
    {
        return UseActions(builder, Assembly.GetEntryAssembly()!);
    }

    public static async IAsyncEnumerable<EndpointAction<TEntity>> GetEndpointActionsAsync<TEntity>(
        this IServiceScope scope)
        where TEntity : Entity
    {
        var types = typeof(TEntity)
            .Assembly
            .GetTypes()
            .Where(c => c.BaseType == typeof(EndpointAction<TEntity>));

        foreach (var type in types)
        {
            if (scope.ServiceProvider.GetRequiredService(type) is not EndpointAction<TEntity> action) continue;
            yield return action;
            await Task.Yield();
        }
    }

    public static async Task<TEntity> InvokeBeforePostAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.BeforePostAsync(entity, cancellationToken);
        }

        return entity;
    }
    
    public static async Task<TEntity> InvokeAfterPostAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default) 
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.AfterPostAsync(entity, cancellationToken);
        }

        return entity;
    }

    public static async Task<TEntity> InvokeBeforePutAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.BeforePutAsync(entity, cancellationToken);
        }

        return entity;
    }

    public static async Task<TEntity> InvokeAfterPutAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.AfterPutAsync(entity, cancellationToken);
        }

        return entity;
    }

    public static async Task<TEntity> InvokeBeforeDeleteAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.BeforeDeleteAsync(entity, cancellationToken);
        }

        return entity;
    }

    public static async Task<TEntity> InvokeAfterDeleteAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.AfterDeleteAsync(entity, cancellationToken);
        }

        return entity;
    }

    public static async Task<TEntity> InvokeAfterGetByIdAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.AfterGetById(entity, cancellationToken);
        }

        return entity;
    }

    public static async Task<List<TEntity>> InvokeAfterGetPaginatedAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, int page, int itemCount, List<TEntity> entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
        {
            entity = await element.AfterGetPaginated(page, itemCount, entity, cancellationToken);
        }

        return entity;
    }
}