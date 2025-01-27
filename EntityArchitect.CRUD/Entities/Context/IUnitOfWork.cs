namespace EntityArchitect.CRUD.Entities.Context;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}