using MediView.BuildingBlocks.Application;
using MediView.Studies.Domain.Studies;
using Microsoft.EntityFrameworkCore;

namespace MediView.Studies.Application.Studies;

public enum StudyActor
{
    Patient,
    Doctor,
    Admin,
}

public sealed record StatusChange(
    StudyStatus? From,
    StudyStatus To,
    DateTimeOffset ChangedAt,
    StudyActor ChangedBy,
    string? Reason);

public sealed record StudyDetail(StudySummary Study, IReadOnlyList<StatusChange> History);

public sealed record GetStudyDetailQuery(Guid StudyId, StudyViewer Viewer) : IQuery<Result<StudyDetail>>;

internal sealed class GetStudyDetailQueryHandler(IStudyReadStore store)
    : IQueryHandler<GetStudyDetailQuery, Result<StudyDetail>>
{
    public async Task<Result<StudyDetail>> Handle(GetStudyDetailQuery request, CancellationToken cancellationToken)
    {
        var readable = request.Viewer.Readable(store.Studies.AsNoTracking())
            .Where(study => study.Id == request.StudyId);

        var summary = await readable.Select(StudySummary.FromStudy).SingleOrDefaultAsync(cancellationToken);
        if (summary is null)
        {
            return Result.Failure<StudyDetail>(StudyErrors.NotFound(request.StudyId));
        }

        var history = await readable
            .SelectMany(study => study.History, (study, entry) => new { study.PatientId, study.DoctorId, Entry = entry })
            .OrderBy(row => row.Entry.ChangedAt)
            .Select(row => new StatusChange(
                row.Entry.FromStatus,
                row.Entry.ToStatus,
                row.Entry.ChangedAt,
                row.Entry.ChangedBy == row.DoctorId ? StudyActor.Doctor
                    : row.Entry.ChangedBy == row.PatientId ? StudyActor.Patient
                    : StudyActor.Admin,
                row.Entry.Reason))
            .ToListAsync(cancellationToken);

        return new StudyDetail(summary, history);
    }
}
