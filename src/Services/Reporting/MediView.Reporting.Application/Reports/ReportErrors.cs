using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;

namespace MediView.Reporting.Application.Reports;

public static class ReportErrors
{
    public static readonly Error StudyNotUpdated = new(
        "The report is finalized, but the study could not be moved. Finalize again to retry.");

    public static Error NotFound(Guid reportId) => new($"Report {reportId} does not exist.");

    public static Error StudyNotFound(Guid studyId) => new($"Study {studyId} does not exist.");

    public static ConflictException NotReader(Guid studyId) =>
        new($"You are not reading study {studyId}. Open the study to write its report.");

    public static ConflictException ChangedConcurrently() =>
        new("The report was saved by another request at the same moment. Save again.");

    public static ConflictException NotAuthor(Guid reportId) =>
        new($"Report {reportId} belongs to another doctor.");
}
