using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public interface IStudyRepository
{
    public Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
