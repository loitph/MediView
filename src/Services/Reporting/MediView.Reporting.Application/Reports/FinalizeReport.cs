using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Application.Reports;

public sealed record FinalizeReportCommand(Guid ReportId, Guid DoctorId, ReportDecision Decision) : ICommand<Result>;

internal sealed class FinalizeReportCommandHandler(
    IReportRepository reports,
    IStudiesClient studies,
    ReportWriter writer,
    TimeProvider timeProvider) : ICommandHandler<FinalizeReportCommand, Result>
{
    public async Task<Result> Handle(FinalizeReportCommand request, CancellationToken cancellationToken)
    {
        var report = await reports.FindAsync(request.ReportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure(ReportErrors.NotFound(request.ReportId));
        }

        if (report.DoctorId != request.DoctorId)
        {
            throw ReportErrors.NotAuthor(request.ReportId);
        }

        if (report.Status == ReportStatus.Draft)
        {
            await writer.EnsureLockOwnerAsync(report.StudyId, request.DoctorId, cancellationToken);
            report.Finalize(request.Decision, timeProvider.GetUtcNow());
            await reports.SaveChangesAsync(cancellationToken);
        }
        else if (report.Decision != request.Decision)
        {
            throw new DomainException($"Report {report.Id} was finalized as {report.Decision} and cannot change.");
        }

        return await studies.TryFinishReadAsync(report.StudyId, request.Decision, cancellationToken)
            ? Result.Success()
            : Result.Failure(ReportErrors.StudyNotUpdated);
    }
}
