using EntityArchitect.CRUD.Results.Abstracts;

namespace EntityArchitect.CRUD.Feature;

public interface ICommandHandler<in TCommand, TResponse> : IBaseCommandHandler
    where TCommand : ICommand<TResponse> where TResponse : class
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);   
}

public interface ICommandHandler<in TCommand> : IBaseCommandHandler
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);   
}