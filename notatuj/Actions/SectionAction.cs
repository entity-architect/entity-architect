using System.Net;
using EntityArchitect.CRUD.Actions;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using notatuj.Entities;
using notatuj.Enumerations;

namespace notatuj.Actions;

public class SectionAction(IRepository<Section> sectionRepository, IClaimProvider claimProvider, IRepository<Note> noteRepository) : EndpointAction<Section>
{
    protected override async ValueTask<Result<Section>> BeforePutAsync(Section entity, CancellationToken cancellationToken = new CancellationToken())
    {
        var userId = Guid.Parse(claimProvider.GetClaims().FirstOrDefault(c => c.Type == "id")?.Value);
        var contribution = await sectionRepository.GetByIdAsync(entity.Id.Value, [nameof(Note)], cancellationToken);
        if(contribution is null)
            return Result.Failure<Section>(Error.NotFound(entity.Id.Value, nameof(Section)));
        
        var note = await noteRepository.GetByIdAsync(contribution.Note.Id.Value, [nameof(Note.Contributors)], cancellationToken);
        if(note is null)
            return Result.Failure<Section>(Error.NotFound(contribution.Note.Id.Value, nameof(Note)));
        
        return !note.Contributors
            .Any(c => c.User.Id == userId && 
                (Equals(c.ContributorType, ContributorType.Creator) || Equals(c.ContributorType, ContributorType.Contributor))) ?
            Result.Failure<Section>(new Error(HttpStatusCode.Forbidden, "You are not allowed to edit this section.")) : 
            entity;
    }
}