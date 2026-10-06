using System.Text.Json.Serialization;

namespace MediView.Web.Api;

[JsonConverter(typeof(JsonStringEnumConverter<StudyStatus>))]
public enum StudyStatus
{
    ToDo,
    InProgress,
    Done,
    ReDiagnosis,
}
