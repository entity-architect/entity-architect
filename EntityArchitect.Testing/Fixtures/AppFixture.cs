using Xunit;
using EntityArchitect.Entities.Context;
using Microsoft.Extensions.DependencyInjection;

namespace EntityArchitect.Testing.Fixtures;


public abstract class AppFixture<TProgram> : IClassFixture<IntegrationTestWebAppFactory<TProgram>> where TProgram : class
{
    private readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;
    protected readonly HttpClient Client;

    protected AppFixture(IntegrationTestWebAppFactory<TProgram> factory)
    {
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Client = factory.CreateClient();
    }
}