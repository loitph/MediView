namespace MediView.Studies.Application.Locking;

public interface IStudyLock
{
    public Task<LockResult> TryAcquire(Guid studyId, LockOwner owner);

    public Task<LockOwner?> GetOwner(Guid studyId);

    public Task<bool> Renew(Guid studyId, Guid doctorId);

    public Task<bool> Release(Guid studyId, Guid doctorId);

    public Task ForceRelease(Guid studyId);
}
