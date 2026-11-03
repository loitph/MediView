using System.Linq.Expressions;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public sealed record StudySummary(
    Guid Id,
    string StudyNumber,
    string PatientName,
    string PatientMrn,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset ScheduledStart,
    StudyStatus Status,
    bool ImagesImported)
{
    internal static readonly Expression<Func<Study, StudySummary>> FromStudy = study => new StudySummary(
        study.Id,
        study.StudyNumber,
        study.PatientName,
        study.PatientMrn,
        study.DoctorId,
        study.DoctorName,
        study.ScheduledStart,
        study.Status,
        study.ImagesImportedAt != null);
}
