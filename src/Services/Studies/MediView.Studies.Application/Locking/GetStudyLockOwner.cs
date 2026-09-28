using MediView.BuildingBlocks.Application;

namespace MediView.Studies.Application.Locking;

public sealed record GetStudyLockOwnerQuery(Guid StudyId) : IQuery<LockOwner?>;

internal sealed class GetStudyLockOwnerQueryHandler(IStudyLock studyLock) : IQueryHandler<GetStudyLockOwnerQuery, LockOwner?>
{
    public Task<LockOwner?> Handle(GetStudyLockOwnerQuery request, CancellationToken cancellationToken) =>
        studyLock.GetOwner(request.StudyId);
}
