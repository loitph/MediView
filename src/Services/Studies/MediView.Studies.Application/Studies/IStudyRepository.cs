using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public interface IStudyRepository
{
    public Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken);

    public void Add(Study study);

    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
