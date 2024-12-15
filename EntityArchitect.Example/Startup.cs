using EntityArchitect.CRUD;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Authorization;
using EntityArchitect.Entities;
using EntityArchitect.Example.Services.Logger;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;


namespace EntityArchitect.Example;


public class Startup
{
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.BuildEntityArchitectAuthorization(typeof(Program).Assembly);
        
        var connectionString = Configuration.GetConnectionString("DefaultConnection");

        services.AddEntityArchitect(typeof(Program).Assembly, connectionString ?? "");
        services.UseActions(typeof(Program).Assembly);
        services.AddAuthorization();
        services.AddScoped<ILogger, Logger>();

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.AddEntityArchitectAuthorization();
        app.MapEntityArchitectCrud(typeof(Program).Assembly);
    }
}
