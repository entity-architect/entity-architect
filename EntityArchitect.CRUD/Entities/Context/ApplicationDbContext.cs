using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntityArchitect.CRUD.Entities.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, Assembly assembly, IConfiguration configuration)
    : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var enumerable = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        foreach (var entity in enumerable) modelBuilder.BuildEntity(entity);
        
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        base.OnModelCreating(modelBuilder);
    }
    
}