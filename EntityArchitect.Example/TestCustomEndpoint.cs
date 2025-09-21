using EntityArchitect.CRUD.Entities.Repository;
using EntityArchitect.CRUD.Feature;
using EntityArchitect.CRUD.Feature.Methods;
using EntityArchitect.CRUD.Results.Abstracts;
using EntityArchitect.CRUD.Services;
using EntityArchitect.Example.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EntityArchitect.Example;

[CRUD.Feature.Route(nameof(Author), "say-hi1")]
public record TestCustomEndpointCommand(Guid AuthorId = default!) : ICommand<string>, IPost;
public class TestCustomEndpointCommandHandler(IRepository<Author> authorRepository) : ICommandHandler<TestCustomEndpointCommand, string>
{
    public async Task<Result<string>> HandleAsync(TestCustomEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var author = await authorRepository.GetByIdAsync(command.AuthorId, cancellationToken);
        return Result.Success("Cześć " + author.Name);    
    }
}

public class TestCustomEndpointCommandValidator : AbstractValidator<TestCustomEndpointCommand>
{
    public TestCustomEndpointCommandValidator()
    {
        RuleFor(x => x.AuthorId).NotEmpty().NotNull();
    }
}