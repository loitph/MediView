using MediView.BuildingBlocks.Application;
using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Application.Reports;

internal sealed class ReportWriter(IReportRepository reports, IStudiesClient studies)
{
    public async Task EnsureReaderAsync(Guid studyId, Guid doctorId, CancellationToken cancellationToken)
    {
        if (await studies.FindReaderAsync(studyId, cancellationToken) != doctorId)
        {
            throw ReportErrors.NotReader(studyId);
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

        await EnsureReaderAsync(report.StudyId, doctorId, cancellationToken);
        return report;
    }
}
