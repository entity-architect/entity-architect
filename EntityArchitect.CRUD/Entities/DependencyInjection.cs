using System.Linq;
using System.Reflection;
using EntityArchitect.CRUD.CustomEndpoints;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Middlewares;
using EntityArchitect.CRUD.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityArchitect.CRUD.Entities;

public static class DependencyInjection
{
    public static IServiceCollection AddEntityArchitect(this IServiceCollection services, Assembly entityAssembly,
        string connectionString)
    {
        services.AddSingleton(entityAssembly);
        services.AddScoped<IClaimProvider, ClaimProvider>();

        services.AddDbContext<ApplicationDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString, builder => { builder.MigrationsAssembly(entityAssembly.FullName); })
                .UseSnakeCaseNamingConvention();
        });

        var enumerable = entityAssembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        foreach (var entity in enumerable)
        {
            var repositoryType = typeof(IRepository<>).MakeGenericType(entity);
            var repositoryImplementationType = typeof(Repository<>).MakeGenericType(entity);

            services.AddScoped(repositoryType, repositoryImplementationType);
        }

        using (var scope = services.BuildServiceProvider().CreateScope())
        {
            if (string.IsNullOrEmpty(connectionString)) return services;
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        }
        foreach (var entity in enumerable)
        {
            var customEndpointType = typeof(CustomEndpoint<>).MakeGenericType(entity);
            entityAssembly.ExportedTypes.Where(c => c.BaseType == customEndpointType).ToList()
                .ForEach(c => services.AddScoped(c));
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ClaimProviderMiddleware>();

        return services;
    }
}