using MediView.BuildingBlocks.Application;

namespace MediView.Studies.Application.Studies;

public static class StudyErrors
{
    public static Error NotFound(Guid studyId) => new($"Study {studyId} does not exist.");
}
