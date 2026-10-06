namespace MediView.Studies.Application.Booking;

public interface IIdentityDirectory
{
    public Task<PatientSnapshot?> FindPatientAsync(Guid patientUserId, CancellationToken cancellationToken);

    public Task<DoctorSnapshot?> FindDoctorAsync(Guid doctorId, CancellationToken cancellationToken);

    public Task<bool> OffersSlotAsync(Guid doctorId, DateTimeOffset start, CancellationToken cancellationToken);
}

public sealed record PatientSnapshot(Guid UserId, string FullName, string Mrn);

public sealed record DoctorSnapshot(Guid Id, string FullName);
