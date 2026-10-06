using MediView.BuildingBlocks.Application;

namespace MediView.Reporting.Application.Reports;

public sealed record ListReportsQuery(Guid StudyId, Guid? DoctorId) : IQuery<Result<IReadOnlyList<ReportView>>>;

internal sealed class ListReportsQueryHandler(IReportRepository reports, IStudiesClient studies)
    : IQueryHandler<ListReportsQuery, Result<IReadOnlyList<ReportView>>>
{
    public async Task<Result<IReadOnlyList<ReportView>>> Handle(ListReportsQuery request, CancellationToken cancellationToken)
    {
        if (!await studies.CanReadAsync(request.StudyId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<ReportView>>(ReportErrors.StudyNotFound(request.StudyId));
        }

        return Result.Success(await reports.ListAsync(request.StudyId, request.DoctorId, cancellationToken));
    }
}
