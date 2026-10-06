using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;
using MediView.Studies.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace MediView.Studies.Infrastructure.Persistence;

public sealed class StudiesDbContext(DbContextOptions<StudiesDbContext> options) : DbContext(options), IStudyReadStore
{
    public const string Schema = "studies";

    public DbSet<Study> Studies => Set<Study>();

    IQueryable<Study> IStudyReadStore.Studies => Studies;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("studies");
        modelBuilder.HasSequence<long>(StudyConfiguration.StudyNumberSequence).StartsAt(1_000);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudiesDbContext).Assembly);
    }
}
