using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Studies;
using Microsoft.EntityFrameworkCore;

namespace MediView.Studies.Application.Booking;

public sealed record ListBookedSlotsQuery(Guid DoctorId, DateTimeOffset From, DateTimeOffset To)
    : IQuery<IReadOnlyList<DateTimeOffset>>;

internal sealed class ListBookedSlotsQueryHandler(IStudyReadStore store)
    : IQueryHandler<ListBookedSlotsQuery, IReadOnlyList<DateTimeOffset>>
{
    public async Task<IReadOnlyList<DateTimeOffset>> Handle(ListBookedSlotsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From.ToUniversalTime();
        var to = request.To.ToUniversalTime();

        return await store.Studies
            .AsNoTracking()
            .Where(study => study.DoctorId == request.DoctorId && study.ScheduledStart >= from && study.ScheduledStart < to)
            .OrderBy(study => study.ScheduledStart)
            .Select(study => study.ScheduledStart)
            .ToListAsync(cancellationToken);
    }
}
