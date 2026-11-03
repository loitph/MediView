namespace MediView.Web.Api;

public sealed record InstanceSummary(Guid Id, string SeriesInstanceUid)
{
    public string FileUrl => $"api/imaging/instances/{Id}/file";
}
