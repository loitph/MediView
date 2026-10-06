using System.Text.Json.Serialization;

namespace MediView.Web.Api;

[JsonConverter(typeof(JsonStringEnumConverter<ReportStatus>))]
public enum ReportStatus
{
    Draft,
    Finalized,
}
