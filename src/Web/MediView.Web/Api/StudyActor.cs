using System.Text.Json.Serialization;

namespace MediView.Web.Api;

[JsonConverter(typeof(JsonStringEnumConverter<StudyActor>))]
public enum StudyActor
{
    Patient,
    Doctor,
    Admin,
}
