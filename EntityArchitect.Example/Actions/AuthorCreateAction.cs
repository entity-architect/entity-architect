using EntityArchitect.CRUD.Actions;
using EntityArchitect.Example.Entities;
using ILogger = EntityArchitect.Example.Services.Logger.ILogger;

namespace EntityArchitect.Example.Actions;

public class AuthorCreateAction(ILogger logger) : EndpointAction<Author>
{
    protected override ValueTask<Author> BeforePostAsync(Author entity, CancellationToken cancellationToken = default)
    {
        entity.AddToName(" added by action");
        logger.Log("Some logic before post");
        return base.BeforePostAsync(entity, cancellationToken);
    }
}