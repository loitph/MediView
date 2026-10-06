namespace MediView.Web.Api;

public sealed record StudyDetail(StudySummary Study, IReadOnlyList<StatusChange> History);
