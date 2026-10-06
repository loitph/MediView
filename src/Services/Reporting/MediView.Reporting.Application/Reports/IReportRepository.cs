using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Application.Reports;

public interface IReportRepository
{
    public Task<Report?> FindAsync(Guid id, CancellationToken cancellationToken);

    public Task<Report?> FindDraftAsync(Guid studyId, Guid doctorId, CancellationToken cancellationToken);

    public Task<IReadOnlyList<ReportView>> ListAsync(Guid studyId, Guid? draftsOfDoctorId, CancellationToken cancellationToken);

    public void Add(Report report);

    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
