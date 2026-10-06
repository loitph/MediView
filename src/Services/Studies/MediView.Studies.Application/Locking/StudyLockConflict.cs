using MediView.BuildingBlocks.Domain;

namespace MediView.Studies.Application.Locking;

internal static class StudyLockConflict
{
    public static ConflictException HeldBy(Guid studyId, LockOwner owner) =>
        new($"Study {studyId} is being read by {owner.DoctorName}.");

    public static ConflictException NotHeldBy(Guid studyId, Guid doctorId) =>
        new($"Doctor {doctorId} does not hold the lock on study {studyId}.");
}
