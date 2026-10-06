namespace MediView.Web.Api;

public sealed record StatusChange(
    StudyStatus? From,
    StudyStatus To,
    DateTimeOffset ChangedAt,
    StudyActor ChangedBy,
    string? Reason);
