namespace MediView.Web.Api;

public sealed record InstanceSummary(Guid Id, string SeriesInstanceUid, int InstanceNumber)
{
    public string FileUrl => $"api/imaging/instances/{Id}/file";
}
