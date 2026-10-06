using MediView.Studies.Application.Booking;
using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MediView.Studies.Infrastructure.Persistence.Repositories;

internal sealed class StudyRepository(StudiesDbContext db) : IStudyRepository
{
    public async Task<Study?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Studies.FindAsync([id], cancellationToken);

    public Task<bool> IsSlotTakenAsync(Guid doctorId, DateTimeOffset scheduledStart, CancellationToken cancellationToken) =>
        db.Studies.AnyAsync(study => study.DoctorId == doctorId && study.ScheduledStart == scheduledStart, cancellationToken);

    public void Add(Study study) => db.Studies.Add(study);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw BookingErrors.SlotTaken();
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
