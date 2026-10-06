namespace MediView.Web.Api;

public sealed record BookAppointmentRequest(Guid DoctorId, DateTimeOffset ScheduledStart);
