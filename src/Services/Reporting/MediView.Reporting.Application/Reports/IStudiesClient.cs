using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Application.Reports;

public interface IStudiesClient
{
    public Task<Guid?> FindLockOwnerAsync(Guid studyId, CancellationToken cancellationToken);

    public Task<bool> CanReadAsync(Guid studyId, CancellationToken cancellationToken);

    public Task<bool> TryFinishReadAsync(Guid studyId, ReportDecision decision, CancellationToken cancellationToken);
}
