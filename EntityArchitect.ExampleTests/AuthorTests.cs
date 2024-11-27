using EntityArchitect.Example;
using EntityArchitect.Testing;
using EntityArchitect.Testing.Fixtures;
using EntityArchitect.Testing.TestAttributes;


namespace EntityArchitect.ExampleTests;

public class AuthorTests(IntegrationTestWebAppFactory<Startup> factory) : AppFixture<Startup>(factory)
{
    [MultiTest<Startup>("integration-test.json")]
    public Task Test() => Client.RunTest<AuthorTests>();
}