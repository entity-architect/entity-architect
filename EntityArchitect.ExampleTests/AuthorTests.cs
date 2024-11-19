using EntityArchitect.Example;
using EntityArchitect.Testing;
using EntityArchitect.Testing.Fixtures;
using EntityArchitect.Testing.TestAttributes;


namespace EntityArchitect.ExampleTests;

public class AuthorTests(IntegrationTestWebAppFactory<Startup> factory) : AppFixture<Startup>(factory)
{
    [SingleTest<Author>("testData.json")]
    public Task Test1() => Client.RunTest<AuthorTests>();
    [MultiTest<Author>("testData1.json")]
    public Task Test2() => Client.RunTest<AuthorTests>();
}