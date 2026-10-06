using System.Text.Json.Serialization;

namespace MediView.Web.Api;

[JsonConverter(typeof(JsonStringEnumConverter<ReportDecision>))]
public enum ReportDecision
{
    Complete,
    ReDiagnosis,
}
