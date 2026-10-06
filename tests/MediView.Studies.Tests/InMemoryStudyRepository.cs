using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Tests;

internal sealed class InMemoryStudyRepository : IStudyRepository
{
    private readonly Dictionary<Guid, Study> _studies = [];

    public int SaveCount { get; private set; }

    public IReadOnlyCollection<Study> All => _studies.Values;

    public void Add(Study study) => _studies.Add(study.Id, study);

    public Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_studies.GetValueOrDefault(id));

    public Task<bool> IsSlotTakenAsync(Guid doctorId, DateTimeOffset scheduledStart, CancellationToken cancellationToken) =>
        Task.FromResult(_studies.Values.Any(study => study.DoctorId == doctorId && study.ScheduledStart == scheduledStart));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
