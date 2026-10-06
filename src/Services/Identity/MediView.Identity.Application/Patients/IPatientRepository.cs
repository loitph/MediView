using MediView.Identity.Domain.Patients;

namespace MediView.Identity.Application.Patients;

public interface IPatientRepository
{
    public Task<PatientSnapshot?> FindSnapshotAsync(Guid userId, CancellationToken cancellationToken);

    public void Add(Patient patient);
}
