using MediView.Reporting.Application.Reports;
using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Tests;

internal sealed class FakeStudies : IStudiesClient
{
    private readonly List<ReportDecision> _finishedReads = [];

    public Guid? LockOwner { get; set; }

    public bool Readable { get; set; } = true;

    public bool StudiesServiceUp { get; set; } = true;

    public IReadOnlyList<ReportDecision> FinishedReads => _finishedReads;

    public Task<Guid?> FindLockOwnerAsync(Guid studyId, CancellationToken cancellationToken) => Task.FromResult(LockOwner);

    public Task<bool> CanReadAsync(Guid studyId, CancellationToken cancellationToken) => Task.FromResult(Readable);

    public Task<bool> TryFinishReadAsync(Guid studyId, ReportDecision decision, CancellationToken cancellationToken)
    {
        if (StudiesServiceUp)
        {
            _finishedReads.Add(decision);
        }

        return Task.FromResult(StudiesServiceUp);
    }
}

internal sealed class InMemoryReportRepository : IReportRepository
{
    private readonly List<Report> _reports = [];

    public int SaveCount { get; private set; }

    public IReadOnlyList<Report> All => _reports;

    public Task<Report?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_reports.SingleOrDefault(report => report.Id == id));

    public Task<Report?> FindDraftAsync(Guid studyId, Guid doctorId, CancellationToken cancellationToken) =>
        Task.FromResult(_reports.SingleOrDefault(report =>
            report.StudyId == studyId && report.DoctorId == doctorId && report.Status == ReportStatus.Draft));

    public Task<IReadOnlyList<ReportView>> ListAsync(Guid studyId, Guid? draftsOfDoctorId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportView>>(
        [
            .. _reports
                .Where(report => report.StudyId == studyId)
                .Where(report => report.Status == ReportStatus.Finalized || report.DoctorId == draftsOfDoctorId)
                .Select(report => new ReportView(
                    report.Id,
                    report.StudyId,
                    report.DoctorName,
                    report.Status,
                    report.Findings,
                    report.Impression,
                    report.Decision,
                    report.FinalizedAt,
                    [])),
        ]);

    public void Add(Report report) => _reports.Add(report);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
