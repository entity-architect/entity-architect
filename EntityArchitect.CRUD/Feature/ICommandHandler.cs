using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Feature;

public interface ICommandHandler<in TCommand, TResponse> : IBaseCommandHandler
    where TCommand : ICommand<TResponse> where TResponse : class
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);   
}