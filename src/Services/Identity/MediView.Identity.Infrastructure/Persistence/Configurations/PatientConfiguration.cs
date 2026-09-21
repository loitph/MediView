using MediView.Identity.Domain.Patients;
using MediView.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediView.Identity.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public const string MrnSequence = "patient_mrn_seq";

    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(patient => patient.Id);
        builder.Property(patient => patient.Mrn)
            .HasMaxLength(16)
            .HasDefaultValueSql($"'MRN' || nextval('{IdentityDbContext.Schema}.{MrnSequence}')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(patient => patient.Mrn).IsUnique();
        builder.HasIndex(patient => patient.UserId).IsUnique();
        builder.HasOne<User>().WithOne().HasForeignKey<Patient>(patient => patient.UserId);
        builder.HasXminConcurrencyToken();
    }
}
