using MediView.BuildingBlocks.Application;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public sealed record OverrideStudyStatusCommand(Guid StudyId, Guid AdminId, StudyStatus To, string Reason) : ICommand<Result>;

internal sealed class OverrideStudyStatusCommandHandler(IStudyRepository studies, TimeProvider timeProvider)
    : ICommandHandler<OverrideStudyStatusCommand, Result>
{
    public async Task<Result> Handle(OverrideStudyStatusCommand request, CancellationToken cancellationToken)
    {
        var study = await studies.FindAsync(request.StudyId, cancellationToken);
        if (study is null)
        {
            return Result.Failure(StudyErrors.NotFound(request.StudyId));
        }

        study.AdminOverride(request.To, request.AdminId, request.Reason, timeProvider.GetUtcNow());
        await studies.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
