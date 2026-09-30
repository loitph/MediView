using MediView.Identity.Application.Patients;
using MediView.Identity.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Infrastructure.Persistence.Repositories;

internal sealed class PatientRepository(IdentityDbContext db) : IPatientRepository
{
    public Task<PatientSnapshot?> FindSnapshotAsync(Guid userId, CancellationToken cancellationToken) =>
        (from patient in db.Patients.AsNoTracking()
         join user in db.Users on patient.UserId equals user.Id
         where patient.UserId == userId
         select new PatientSnapshot(user.Id, user.FullName, patient.Mrn))
        .SingleOrDefaultAsync(cancellationToken);

    public void Add(Patient patient) => db.Patients.Add(patient);
}
