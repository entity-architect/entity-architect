using System.Net;
using EntityArchitect.CRUD.CustomEndpoints;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using EntityArchitect.Example.Entities;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;

namespace EntityArchitect.Example;

public class TestCustomEndpoint(IClaimProvider claimProvider, IRepository<Client> clientRepository) : CustomEndpoint<Client>
{
    [CustomEndpoint("POST", "Test")]
    public async Task<Result<string>> Test()
    {
        var id = Guid.Parse(
            claimProvider.GetClaims()
                .FirstOrDefault(c => c.Type == "id")?.Value!);
        
        var client = await clientRepository.GetByIdAsync(id);
        
        return Result.Success("Cześć " + client.Name);
    }
}