using EntityArchitect.CRUD;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Entities;
using EntityArchitect.Example.Services.Logger;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;


namespace EntityArchitect.Example;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var connectionString = Configuration.GetConnectionString("DefaultConnection");

        services.AddEntityArchitect(typeof(Program).Assembly, connectionString ?? "");
        services.UseActions(typeof(Program).Assembly);
        services.AddScoped<ILogger, Logger>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapEntityArchitectCrud(typeof(Program).Assembly);
    }
}