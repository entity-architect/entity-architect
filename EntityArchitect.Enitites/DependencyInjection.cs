using System.Reflection;
using EntityArchitect.Entities.Context;
using EntityArchitect.Entities.Entities;
using EntityArchitect.Entities.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntityArchitect.Entities;

public static class DependencyInjection
{
    public static IServiceCollection AddEntityArchitect(this IServiceCollection services, Assembly entityAssembly, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddSingleton(entityAssembly);
        
        services.AddDbContext<ApplicationDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString, builder =>
            {
                builder.MigrationsAssembly(entityAssembly.FullName);
            }).UseSnakeCaseNamingConvention();
        });
        
        var enumerable = entityAssembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        foreach (var entity in enumerable)
        {
            var repositoryType = typeof(IRepository<>).MakeGenericType(entity);
            var repositoryImplementationType = typeof(Repository<>).MakeGenericType(entity);
            
            services.AddScoped(repositoryType, repositoryImplementationType);
        }
        
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        
        return services;
    }
}

