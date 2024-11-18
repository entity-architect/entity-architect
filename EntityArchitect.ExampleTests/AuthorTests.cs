using EntityArchitect.Example;
using EntityArchitect.Testing;
using EntityArchitect.Testing.Fixtures;


namespace EntityArchitect.ExampleTests;

public class AuthorTests(IntegrationTestWebAppFactory<Startup> factory) : AppFixture<Startup>(factory)
{
    [Test<Author>("testData.json")]
    public Task Test1() => Client.RunTest<AuthorTests>();
    [Test<Author>("testData1.json")]
    public Task Test2() => Client.RunTest<AuthorTests>();
}