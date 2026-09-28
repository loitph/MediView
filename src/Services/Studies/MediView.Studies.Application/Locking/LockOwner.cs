namespace MediView.Studies.Application.Locking;

public sealed record LockOwner(Guid DoctorId, string DoctorName);
