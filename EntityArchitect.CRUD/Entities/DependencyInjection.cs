using System.Reflection;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;
using Microsoft.EntityFrameworkCore;

namespace EntityArchitect.CRUD.Entities;

public static class DependencyInjection
{
    public static IServiceCollection AddEntityArchitect(this IServiceCollection services, Assembly entityAssembly,
        string connectionString)
    {
        services.AddSingleton(entityAssembly);

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

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        return services;
    }
}