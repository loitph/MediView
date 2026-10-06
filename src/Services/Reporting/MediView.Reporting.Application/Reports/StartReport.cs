using MediView.BuildingBlocks.Application;
using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Application.Reports;

public sealed record StartReportCommand(Guid StudyId, Guid DoctorId, string DoctorName) : ICommand<Guid>;

internal sealed class StartReportCommandHandler(IReportRepository reports, ReportWriter writer)
    : ICommandHandler<StartReportCommand, Guid>
{
    public async Task<Guid> Handle(StartReportCommand request, CancellationToken cancellationToken)
    {
        await writer.EnsureLockOwnerAsync(request.StudyId, request.DoctorId, cancellationToken);

        if (await reports.FindDraftAsync(request.StudyId, request.DoctorId, cancellationToken) is { } draft)
        {
            return draft.Id;
        }

        var report = Report.Draft(request.StudyId, request.DoctorId, request.DoctorName);
        reports.Add(report);
        await reports.SaveChangesAsync(cancellationToken);
        return report.Id;
    }
}
