namespace MediView.Studies.Application.Locking;

public sealed record LockResult(bool Acquired, LockOwner CurrentOwner);
