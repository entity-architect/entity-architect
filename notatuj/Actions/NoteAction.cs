using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using notatuj.Entities;
using notatuj.Enumerations;

namespace notatuj.Actions;

public class NoteAction(IRepository<User> userRepository, IClaimProvider claimProvider) : EndpointAction<Note>
{
    protected override async ValueTask<Result<Note>> BeforePostAsync(Note entity, CancellationToken cancellationToken = new CancellationToken())
    {
        var userId = Guid.Parse(claimProvider.GetClaims().FirstOrDefault(c => c.Type == "id")?.Value);
        var user = await userRepository.GetByIdAsync(userId, [], cancellationToken);
        if(user is null)
            return Result.Failure<Note>(Error.NotFound(userId, nameof(User)));
        
        entity.Contributors = new List<Contribution> { new() { Note = entity, ContributorType = ContributorType.Creator, User = user} };
        
        return entity;
    }
}