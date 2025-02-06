using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EntityArchitect.CRUD.Actions;

public static class ActionsBuilder
{
    public static IServiceCollection UseActions(this IServiceCollection services, Assembly assembly)
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
                services.AddScoped(action));
        }

        return services;
    }

    public static IServiceCollection UseActions(this IServiceCollection services)
    {
        return UseActions(services, Assembly.GetEntryAssembly()!);
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
            entity = await element.BeforePostAsync(entity, cancellationToken);

        return entity;
    }

    public static async Task<TEntity> InvokeAfterPostAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
            entity = await element.AfterPostAsync(entity, cancellationToken);

        return entity;
    }

    public static async Task<TEntity> InvokeBeforePutAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
            entity = await element.BeforePutAsync(entity, cancellationToken);

        return entity;
    }

    public static async Task<TEntity> InvokeAfterPutAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
            entity = await element.AfterPutAsync(entity, cancellationToken);

        return entity;
    }

    public static async Task<TEntity> InvokeBeforeDeleteAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
            entity = await element.BeforeDeleteAsync(entity, cancellationToken);

        return entity;
    }

    public static async Task<TEntity> InvokeAfterDeleteAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
            entity = await element.AfterDeleteAsync(entity, cancellationToken);

        return entity;
    }

    public static async Task<TEntity> InvokeAfterGetByIdAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
            entity = await element.AfterGetById(entity, cancellationToken);

        return entity;
    }

    public static async Task<List<TEntity>> InvokeAfterGetPaginatedAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, int page, int itemCount, List<TEntity> entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        await foreach (var element in list.WithCancellation(cancellationToken))
            entity = await element.AfterGetPaginated(page, itemCount, entity, cancellationToken);

        return entity;
    }
}