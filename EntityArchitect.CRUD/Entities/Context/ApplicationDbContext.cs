using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EntityArchitect.CRUD.Application;
using EntityArchitect.CRUD.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace EntityArchitect.CRUD.Entities.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, Assembly assembly, IConfiguration configuration)
    : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var enumerable = assembly.ExportedTypes.Where(c => c.BaseType == typeof(Entity)).ToList();
        foreach (var entity in enumerable) modelBuilder.BuildEntity(entity);

        modelBuilder.Entity<EndpointMap>(b =>
        {
            b.ToTable("__EndpointMap");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            b.Property(c => c.Accesses).HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<ICollection<Access>>(v)!);
        });
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        base.OnModelCreating(modelBuilder);
    }
    
}