using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Studies.Application.Locking;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public enum ReadDecision
{
    Complete,
    ReDiagnosis,
}

public sealed record FinishReadCommand(Guid StudyId, Guid DoctorId, ReadDecision Decision) : ICommand<Result>;

internal sealed class FinishReadCommandHandler(
    IStudyRepository studies,
    IStudyLock studyLock,
    TimeProvider timeProvider) : ICommandHandler<FinishReadCommand, Result>
{
    public async Task<Result> Handle(FinishReadCommand request, CancellationToken cancellationToken)
    {
        var (studyId, doctorId, decision) = request;

        var study = await studies.FindAsync(studyId, cancellationToken);
        if (study is null)
        {
            return Result.Failure(StudyErrors.NotFound(studyId));
        }

        if (study.DoctorId != doctorId)
        {
            throw new DomainException($"Doctor {doctorId} is not assigned to study {studyId}.");
        }

        if (study.Status != Outcome(decision))
        {
            Apply(study, decision, timeProvider.GetUtcNow());
            await studies.SaveChangesAsync(cancellationToken);
        }

        await studyLock.Release(studyId, doctorId);
        return Result.Success();
    }

    private static StudyStatus Outcome(ReadDecision decision) =>
        decision == ReadDecision.Complete ? StudyStatus.Done : StudyStatus.ReDiagnosis;

    private static void Apply(Study study, ReadDecision decision, DateTimeOffset now)
    {
        if (decision == ReadDecision.Complete)
        {
            study.Complete(now);
        }
        else
        {
            study.RequestReDiagnosis(now);
        }
    }
}
