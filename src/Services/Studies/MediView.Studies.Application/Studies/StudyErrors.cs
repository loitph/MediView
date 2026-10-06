using MediView.BuildingBlocks.Application;

namespace MediView.Studies.Application.Studies;

public static class StudyErrors
{
    public static Error NotFound(Guid studyId) => new("study.not_found", $"Study {studyId} does not exist.");
}
