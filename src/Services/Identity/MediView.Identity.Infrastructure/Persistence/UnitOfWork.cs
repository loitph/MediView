using MediView.BuildingBlocks.Domain;
using MediView.Identity.Application;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MediView.Identity.Infrastructure.Persistence;

internal sealed class UnitOfWork(IdentityDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        })
        {
            throw new ConflictException("That email or licence number is already registered.");
        }
    }
}
