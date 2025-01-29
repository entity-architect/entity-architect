using EntityArchitect.CRUD;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Authorization;
using EntityArchitect.CRUD.Entities;
using EntityArchitect.Example.Services.Logger;
using Microsoft.OpenApi.Models;
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
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Wpisz 'Bearer' oraz spację, a następnie token JWT."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        var connectionString = Configuration.GetConnectionString("DefaultConnection");

        services.AddScoped<ILogger, Logger>();
        services.AddEntityArchitect(typeof(Program).Assembly, connectionString ?? "");
        services.BuildEntityArchitectAuthorization(typeof(Program).Assembly);
        services.UseActions(typeof(Program).Assembly);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapEntityArchitectCrud(typeof(Program).Assembly);
    }
}