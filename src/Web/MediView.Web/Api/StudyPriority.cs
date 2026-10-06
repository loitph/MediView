using System.Text.Json.Serialization;

namespace MediView.Web.Api;

[JsonConverter(typeof(JsonStringEnumConverter<StudyPriority>))]
public enum StudyPriority
{
    Routine,
    Urgent,
    Stat,
}
