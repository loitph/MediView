using MediView.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;

namespace MediView.Studies.Application.Studies;

public sealed record ListStudiesQuery(StudyViewer Viewer) : IQuery<IReadOnlyList<StudySummary>>;

internal sealed class ListStudiesQueryHandler(IStudyReadStore store)
    : IQueryHandler<ListStudiesQuery, IReadOnlyList<StudySummary>>
{
    public async Task<IReadOnlyList<StudySummary>> Handle(ListStudiesQuery request, CancellationToken cancellationToken) =>
        await request.Viewer.Listed(store.Studies.AsNoTracking())
            .InWorklistOrder()
            .Select(StudySummary.FromStudy)
            .ToListAsync(cancellationToken);
}
