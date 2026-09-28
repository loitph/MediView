using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Tests;

internal sealed class InMemoryStudyRepository : IStudyRepository
{
    private readonly Dictionary<Guid, Study> _studies = [];

    public int SaveCount { get; private set; }

    public void Add(Study study) => _studies.Add(study.Id, study);

    public Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_studies.GetValueOrDefault(id));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
