namespace MediView.Identity.Application;

public interface IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
