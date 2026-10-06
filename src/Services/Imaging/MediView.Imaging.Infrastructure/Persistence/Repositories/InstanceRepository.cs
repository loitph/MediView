using MediView.Imaging.Application.Instances;
using MediView.Imaging.Domain.Instances;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MediView.Imaging.Infrastructure.Persistence.Repositories;

internal sealed class InstanceRepository(ImagingDbContext db) : IInstanceRepository
{
    public async Task<IReadOnlySet<string>> SopInstanceUidsOfAsync(Guid studyId, CancellationToken cancellationToken) =>
        await db.Instances
            .Where(instance => instance.StudyId == studyId)
            .Select(instance => instance.SopInstanceUid)
            .ToHashSetAsync(cancellationToken);

    public async Task<IReadOnlyList<InstanceSummary>> ListAsync(Guid studyId, CancellationToken cancellationToken) =>
        await db.Instances
            .AsNoTracking()
            .Where(instance => instance.StudyId == studyId)
            .OrderBy(instance => instance.SeriesInstanceUid)
            .ThenBy(instance => instance.InstanceNumber)
            .Select(instance => new InstanceSummary(instance.Id, instance.SeriesInstanceUid, instance.InstanceNumber))
            .ToListAsync(cancellationToken);

    public Task<string?> FindStoragePathAsync(Guid instanceId, CancellationToken cancellationToken) =>
        db.Instances
            .Where(instance => instance.Id == instanceId)
            .Select(instance => instance.StoragePath)
            .SingleOrDefaultAsync(cancellationToken);

    public void Add(Instance instance) => db.Instances.Add(instance);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw ImagingErrors.ImportInProgress();
        }
    }
}
