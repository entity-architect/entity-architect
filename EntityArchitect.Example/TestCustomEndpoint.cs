using System.Net;
using EntityArchitect.CRUD.CustomEndpoints;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.Example.Entities;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;

namespace EntityArchitect.Example;

public class TestCustomEndpoint(IRepository<Author> authorRepository) : CustomEndpoint<Author>
{
    [CustomEndpoint("POST", "Test")]
    public async Task<Result<string>> Test(string name)
    {
        if (authorRepository is null)
        {
            return Result.Failure<string>(new Error(HttpStatusCode.NotFound, "AuthorRepositoryNotFound"));
        }
        return Result.Success("Cześć " + name);
    }
}