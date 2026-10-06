using MediView.Web.Api;

namespace MediView.Web.Components;

public static class StudyLabels
{
    public static string Label(this StudyStatus status) => status switch
    {
        StudyStatus.ToDo => "To do",
        StudyStatus.InProgress => "In progress",
        StudyStatus.ReDiagnosis => "Re-diagnosis",
        StudyStatus.Done => "Done",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static string CssModifier(this StudyStatus status) => status switch
    {
        StudyStatus.ToDo => "todo",
        StudyStatus.InProgress => "progress",
        StudyStatus.ReDiagnosis => "rediagnosis",
        StudyStatus.Done => "done",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static string CssModifier(this StudyPriority priority) => priority switch
    {
        StudyPriority.Routine => "routine",
        StudyPriority.Urgent => "urgent",
        StudyPriority.Stat => "stat",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
    };
}
