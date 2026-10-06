using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public interface IStudyRepository
{
    public Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken);

    public Task<bool> IsSlotTakenAsync(Guid doctorId, DateTimeOffset scheduledStart, CancellationToken cancellationToken);

    public void Add(Study study);

    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
