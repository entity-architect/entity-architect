using System.Reflection;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Entities;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Feature;
using EntityArchitect.CRUD.Files;
using EntityArchitect.CRUD.Services;
using Microsoft.EntityFrameworkCore;

namespace EntityArchitect.CRUD.Entities;

public static class DependencyInjection
{
    public static IServiceCollection AddEntityArchitect(this IServiceCollection services, Assembly entityAssembly,
        string connectionString)
    {
        services.AddSingleton(entityAssembly);
        services.AddScoped<IClaimProvider, ClaimProvider>();
        services.AddHttpContextAccessor();
    
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
            var customEndpointType = typeof(ICommand<>).MakeGenericType(entity);
            entityAssembly.ExportedTypes.Where(c => c.BaseType == customEndpointType).ToList()
                .ForEach(c => services.AddScoped(c));
        }

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddAntiforgery(); 
        services.AddTransient<IFileService, FileService>();
        
        var assembly = Assembly.GetEntryAssembly()!;
        var types = assembly.GetTypes().Where(c => c.GetInterfaces()
            .Contains(typeof(IBaseCommand))).ToList();
        var handlers = assembly.GetTypes().Where(c => c.GetInterfaces()
            .Contains(typeof(IBaseCommandHandler)) && c is { IsInterface: false, IsAbstract: false }).ToList();
        List<IBaseCommandHandler> endpoints = [];
        foreach (var item in types)
        {
            var handlerType = handlers.FirstOrDefault(c => c.GetInterfaces().Skip(c.GetInterfaces().Length - 2).First().GetGenericArguments()[0] == item);
            if (handlerType == null) throw new InvalidOperationException($"Handler for {item.Name} not found.");
            services.AddScoped(handlerType);
        }

        return services;
    }
    
}