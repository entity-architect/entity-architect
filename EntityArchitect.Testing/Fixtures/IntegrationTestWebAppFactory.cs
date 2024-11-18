using System.Globalization;
using EntityArchitect.Entities;
using EntityArchitect.Entities.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace EntityArchitect.Testing.Fixtures
{
    public class IntegrationTestWebAppFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("db")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithPortBinding(55000)
            .Build();

        public Task InitializeAsync()
        {
            return _dbContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.AddEnvironmentVariables();
            });

            builder.ConfigureTestServices(services =>
            {
                var connectionString = _dbContainer.GetConnectionString();
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options => 
                    options.UseNpgsql(connectionString)
                        .UseSnakeCaseNamingConvention());
                
                using (var scope = services.BuildServiceProvider().CreateScope())
                {
                    if (string.IsNullOrEmpty(connectionString)) return;
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    dbContext.Database.EnsureCreated();
                }

            });
        }

        public Task DisposeAsync()
        {
            return _dbContainer.StopAsync();
        }

        public IntegrationTestWebAppFactory()
        {
            
        }
    }
}