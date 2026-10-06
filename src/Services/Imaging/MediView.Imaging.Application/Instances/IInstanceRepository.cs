using MediView.Imaging.Domain.Instances;

namespace MediView.Imaging.Application.Instances;

public interface IInstanceRepository
{
    public Task<IReadOnlySet<string>> SopInstanceUidsOfAsync(Guid studyId, CancellationToken cancellationToken);

    public Task<IReadOnlyList<InstanceSummary>> ListAsync(Guid studyId, CancellationToken cancellationToken);

    public Task<string?> FindStoragePathAsync(Guid instanceId, CancellationToken cancellationToken);

    public void Add(Instance instance);

    public Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record InstanceSummary(Guid Id, string SeriesInstanceUid, int InstanceNumber);
