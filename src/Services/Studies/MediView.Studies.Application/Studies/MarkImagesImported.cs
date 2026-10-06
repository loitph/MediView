using MediView.BuildingBlocks.Application;

namespace MediView.Studies.Application.Studies;

public sealed record MarkImagesImportedCommand(Guid StudyId) : ICommand<Result>;

internal sealed class MarkImagesImportedCommandHandler(IStudyRepository studies, TimeProvider timeProvider)
    : ICommandHandler<MarkImagesImportedCommand, Result>
{
    public async Task<Result> Handle(MarkImagesImportedCommand request, CancellationToken cancellationToken)
    {
        var study = await studies.FindAsync(request.StudyId, cancellationToken);
        if (study is null)
        {
            return Result.Failure(StudyErrors.NotFound(request.StudyId));
        }

        if (study.ImagesImportedAt is null)
        {
            study.MarkImagesImported(timeProvider.GetUtcNow());
            await studies.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
