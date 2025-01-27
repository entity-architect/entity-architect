using EntityArchitect.CRUD.Testing;
using EntityArchitect.CRUD.Testing.Fixtures;
using EntityArchitect.CRUD.Testing.TestAttributes;
using EntityArchitect.Example;

namespace EntityArchitect.ExampleTests;

public class AuthorTests(IntegrationTestWebAppFactory<Startup> factory) : AppFixture<Startup>(factory)
{
    [MultiTest<Startup>("integration-test.json")]
    public Task Test()
    {
        return Client.RunTest<AuthorTests>();
    }
}