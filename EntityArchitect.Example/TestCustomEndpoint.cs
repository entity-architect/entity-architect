using EntityArchitect.CRUD.Authorization.Attributes;
using EntityArchitect.CRUD.CustomEndpoints;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using EntityArchitect.Example.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EntityArchitect.Example;

public class TestCustomEndpoint(IClaimProvider claimProvider, IRepository<Client> clientRepository) : Feature<Client>
{
    [Feature("POST", "Test"), Secured(typeof(Client))]
    public async Task<Result<string>> Test([FromBody] Author text, CancellationToken cancellationToken)
    {
        var claims = claimProvider.GetClaims();
        var id = Guid.Parse(claims.FirstOrDefault(c => c.Type == "id")?.Value!);
        
        var client = await clientRepository.GetByIdAsync(id, cancellationToken);
        
        return Result.Success("Cześć " + client.Name + " " + text);
    }
}