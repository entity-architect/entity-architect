using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Results.Abstracts;
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

    public static async Task<Result<TEntity>> InvokeBeforePostAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<TEntity> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.BeforePostAsync(entity, cancellationToken);

        return result;
    }


    public static async Task<Result<TEntity>> InvokeAfterPostAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<TEntity> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.AfterPostAsync(result.Value, cancellationToken);

        return result;
    }

    public static async Task<Result<TEntity>> InvokeBeforePutAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<TEntity> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.BeforePutAsync(result.Value, cancellationToken);

        return result;
    }

    public static async Task<Result<TEntity>> InvokeAfterPutAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<TEntity> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.AfterPutAsync(result.Value, cancellationToken);

        return result;
    }

    public static async Task<Result<TEntity>> InvokeBeforeDeleteAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<TEntity> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.BeforeDeleteAsync(result.Value, cancellationToken);

        return result;
    }

    public static async Task<Result<TEntity>> InvokeAfterDeleteAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<TEntity> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.AfterDeleteAsync(result.Value, cancellationToken);

        return result;
    }

    public static async Task<Result<TEntity>> InvokeAfterGetByIdAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<TEntity> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.AfterGetById(result.Value, cancellationToken);

        return result;
    }

    public static async Task<Result<List<TEntity>>> InvokeAfterGetPaginatedAsync<TEntity>(
        this IAsyncEnumerable<EndpointAction<TEntity>> list, int page, int itemCount, List<TEntity> entity,
        CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        Result<List<TEntity>> result = entity;
        await foreach (var element in list.WithCancellation(cancellationToken))
            result = await element.AfterGetPaginated(page, itemCount, result.Value, cancellationToken);

        return result;
    }
}