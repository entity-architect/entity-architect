using System.Net;
using EntityArchitect.CRUD.CustomEndpoints;
using EntityArchitect.CRUD.Entities.Context;
using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using Microsoft.AspNetCore.Mvc;
using notatuj.Entities;
using notatuj.Enumerations;

namespace notatuj.Endpoints;

public class ContributorEndpoint(IRepository<Contribution> contributorRepository, IUnitOfWork unitOfWork, IClaimProvider claimProvider, IRepository<User> userRepository, IRepository<Note> noteRepository) : CustomEndpoint<Contribution>
{
    [CustomEndpoint("POST", "accept")]
    public async Task<Result> AcceptContributor([FromBody] ContributionRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(claimProvider.GetClaims().FirstOrDefault(c => c.Type == "id")?.Value);
        var user = await userRepository.GetByIdAsync(userId, [], cancellationToken);
        
        if(user is null)
            return Result.Failure(Error.NotFound(userId, nameof(User)));
        
        var contribution = await contributorRepository.GetByIdAsync(request.ContributionId, [nameof(Contribution.Note)], cancellationToken);
        
        if(contribution is null)
            return Result.Failure(Error.NotFound(request.ContributionId, nameof(Contribution)));
        
        var note = await noteRepository.GetByIdAsync(contribution.Note.Id.Value, [nameof(Note.Contributors)], cancellationToken);
        
        if(note is null)
            return Result.Failure(Error.NotFound(contribution.Note.Id.Value, nameof(Note)));
        
        
        var creator = note.Contributors.FirstOrDefault(c => c.User.Id == userId && c.ContributorType == ContributorType.Creator);
        if (creator is null)
            return Result.Failure(new Error(HttpStatusCode.Forbidden, "You are not allowed to accept this contribution."));
        else
        {
            contribution.ContributorType = ContributorType.Contributor;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
    
    [CustomEndpoint("POST", "reject")]
    public async Task<Result> RejectContributor([FromBody] ContributionRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(claimProvider.GetClaims().FirstOrDefault(c => c.Type == "id")?.Value);
        var user = await userRepository.GetByIdAsync(userId, [], cancellationToken);
        
        if(user is null)
            return Result.Failure(Error.NotFound(userId, nameof(User)));
        
        var contribution = await contributorRepository.GetByIdAsync(request.ContributionId, [nameof(Contribution.Note)], cancellationToken);
        
        if(contribution is null)
            return Result.Failure(Error.NotFound(request.ContributionId, nameof(Contribution)));
        
        var note = await noteRepository.GetByIdAsync(contribution.Note.Id.Value, [nameof(Note.Contributors)], cancellationToken);
        
        if(note is null)
            return Result.Failure(Error.NotFound(contribution.Note.Id.Value, nameof(Note)));
        
        
        var creator = note.Contributors.FirstOrDefault(c => c.User.Id == userId && c.ContributorType == ContributorType.Creator);
        if (creator is null)
            return Result.Failure(new Error(HttpStatusCode.Forbidden, "You are not allowed to accept this contribution."));
        else
        {
            contributorRepository.Remove(contribution);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
    
    public class ContributionRequest
    {
        public Guid ContributionId { get; set; }
    }
}