using System.Net.Http;
using EntityArchitect.CRUD.Entities.Context;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EntityArchitect.CRUD.Testing.Fixtures;

public abstract class AppFixture<TProgram> : IClassFixture<IntegrationTestWebAppFactory<TProgram>>
    where TProgram : class
{
    protected readonly HttpClient Client;
    protected readonly ApplicationDbContext DbContext;
    private readonly IServiceScope Scope;

    protected AppFixture(IntegrationTestWebAppFactory<TProgram> factory)
    {
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Client = factory.CreateClient();
    }
}