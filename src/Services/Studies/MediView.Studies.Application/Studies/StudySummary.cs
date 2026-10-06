using System.Linq.Expressions;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public sealed record StudySummary(
    Guid Id,
    string StudyNumber,
    string PatientName,
    string PatientMrn,
    string DoctorName,
    DateTimeOffset ScheduledStart,
    StudyPriority Priority,
    StudyStatus Status,
    bool ImagesImported)
{
    internal static readonly Expression<Func<Study, StudySummary>> FromStudy = study => new StudySummary(
        study.Id,
        study.StudyNumber,
        study.PatientName,
        study.PatientMrn,
        study.DoctorName,
        study.ScheduledStart,
        study.Priority,
        study.Status,
        study.ImagesImportedAt != null);
}

public static class WorklistOrdering
{
    public static IOrderedQueryable<Study> InWorklistOrder(this IQueryable<Study> studies) =>
        studies
            .OrderBy(study => study.Priority == StudyPriority.Stat ? 0 : study.Priority == StudyPriority.Urgent ? 1 : 2)
            .ThenBy(study => study.ScheduledStart);
}
