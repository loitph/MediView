using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Infrastructure.Persistence.Repositories;

internal sealed class StudyRepository(StudiesDbContext db) : IStudyRepository
{
    public async Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Studies.FindAsync([id], cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
