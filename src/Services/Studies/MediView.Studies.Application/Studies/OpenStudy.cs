using MediView.BuildingBlocks.Application;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public sealed record OpenStudyCommand(Guid StudyId, Guid DoctorId) : ICommand<Result>;

internal sealed class OpenStudyCommandHandler(
    IStudyRepository studies,
    TimeProvider timeProvider) : ICommandHandler<OpenStudyCommand, Result>
{
    public async Task<Result> Handle(OpenStudyCommand request, CancellationToken cancellationToken)
    {
        var (studyId, doctorId) = request;

        var study = await studies.FindAsync(studyId, cancellationToken);
        if (study is null)
        {
            return Result.Failure(StudyErrors.NotFound(studyId));
        }

        if (IsAlreadyReadBy(study, doctorId))
        {
            return Result.Success();
        }

        study.StartRead(doctorId, timeProvider.GetUtcNow());
        await studies.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool IsAlreadyReadBy(Study study, Guid doctorId) =>
        study.Status == StudyStatus.InProgress && study.DoctorId == doctorId;
}
