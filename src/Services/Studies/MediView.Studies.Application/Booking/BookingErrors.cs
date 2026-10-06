using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;

namespace MediView.Studies.Application.Booking;

public static class BookingErrors
{
    public static readonly Error OutsideSchedule = new(
        "booking.outside_schedule",
        "The doctor does not offer that slot. Pick a time from their availability.");

    public static Error PatientNotFound(Guid patientUserId) =>
        new("booking.patient_not_found", $"User {patientUserId} is not a registered patient.");

    public static Error DoctorNotFound(Guid doctorId) =>
        new("booking.doctor_not_found", $"Doctor {doctorId} does not exist.");

    public static ConflictException SlotTaken() => new("That slot is already booked. Pick another time.");
}
