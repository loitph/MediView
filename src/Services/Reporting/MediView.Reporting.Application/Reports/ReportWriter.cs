using MediView.BuildingBlocks.Application;
using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Application.Reports;

internal sealed class ReportWriter(IReportRepository reports, IStudiesClient studies)
{
    public async Task EnsureLockOwnerAsync(Guid studyId, Guid doctorId, CancellationToken cancellationToken)
    {
        if (await studies.FindLockOwnerAsync(studyId, cancellationToken) != doctorId)
        {
            throw ReportErrors.NotLockOwner(studyId);
        }
    }

    public async Task<Result<Report>> OpenForWritingAsync(Guid reportId, Guid doctorId, CancellationToken cancellationToken)
    {
        var report = await reports.FindAsync(reportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure<Report>(ReportErrors.NotFound(reportId));
        }

        if (report.DoctorId != doctorId)
        {
            throw ReportErrors.NotAuthor(reportId);
        }

        await EnsureLockOwnerAsync(report.StudyId, doctorId, cancellationToken);
        return report;
    }
}
