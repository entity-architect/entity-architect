using System.Reflection;
using EntityArchitect.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntityArchitect.Entities.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, Assembly assembly) : DbContext(options) , IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var enumerable = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        foreach (var entity in enumerable)
        {
            modelBuilder.BuildEntity(entity);
        }
        
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        base.OnModelCreating(modelBuilder);
    }
}