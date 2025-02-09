using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Services;
using EntityArchitect.Example.Entities;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;

namespace EntityArchitect.Example.Actions;

public class AuthorCreateAction(ILogger logger, IClaimProvider claimProvider, IRepository<Client> clientRepository, IUnitOfWork unitOfWork, IRepository<Author> authorRepository) : EndpointAction<Author>
{
    protected override async ValueTask<Author> BeforePostAsync(Author entity, CancellationToken cancellationToken = default)
    {
        Console.WriteLine(claimProvider.GetHashCode());
        var claims = Guid.Parse(claimProvider.GetClaims().FirstOrDefault(c => c.Type == "id")!.Value);
        var client = await clientRepository.GetByIdAsync(claims,null, cancellationToken);
        if (client == null)
        {
            logger.Log("Client not found");
            return entity;
        }
        
        entity.AddToName(" added by " + client.Name);

        var author = new Author()
        {
            Name = "aaa"
        };
        
        await authorRepository.AddAsync(author, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        
        return entity;
    }
}