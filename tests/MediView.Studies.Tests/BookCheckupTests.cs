using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Studies.Application.Booking;
using MediView.Studies.Domain.Studies;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class BookCheckupTests : IAsyncDisposable
{
    private static readonly PatientSnapshot Patient = new(Guid.NewGuid(), "Patient A", "MRN-1042");
    private static readonly DoctorSnapshot Doctor = new(Guid.NewGuid(), "Dr. B");
    private static readonly DateTimeOffset OfferedSlot = new(2026, 10, 5, 9, 0, 0, TimeSpan.FromHours(7));

    private readonly InMemoryStudyRepository _studies = new();
    private readonly FakeIdentityDirectory _identity = new(Patient, Doctor, OfferedSlot);
    private readonly StudiesApplication _application;

    public BookCheckupTests() => _application = new StudiesApplication(_identity, _studies);

    public ValueTask DisposeAsync() => _application.DisposeAsync();

    [Fact]
    public async Task BookingCopiesPatientAndDoctorSnapshotsOntoATodoStudy()
    {
        var result = await Book(Doctor.Id, OfferedSlot);

        Assert.True(result.IsSuccess);
        var study = Assert.Single(_studies.All);
        Assert.Equal(result.Value.Id, study.Id);
        Assert.Equal(Patient.FullName, study.PatientName);
        Assert.Equal(Patient.Mrn, study.PatientMrn);
        Assert.Equal(Doctor.FullName, study.DoctorName);
        Assert.Equal(StudyStatus.ToDo, study.Status);
        Assert.Equal(StudyPriority.Routine, study.Priority);
    }

    [Fact]
    public async Task BookingStoresTheSlotStartAsUtc()
    {
        await Book(Doctor.Id, OfferedSlot);

        var study = Assert.Single(_studies.All);
        Assert.Equal(TimeSpan.Zero, study.ScheduledStart.Offset);
        Assert.Equal(OfferedSlot, study.ScheduledStart);
    }

    [Fact]
    public async Task BookingTheSameSlotTwiceIsAConflict()
    {
        await Book(Doctor.Id, OfferedSlot);

        await Assert.ThrowsAsync<ConflictException>(() => Book(Doctor.Id, OfferedSlot));

        Assert.Single(_studies.All);
    }

    [Fact]
    public async Task BookingOutsideTheDoctorsScheduleIsRejected()
    {
        var result = await Book(Doctor.Id, OfferedSlot.AddMinutes(10));

        Assert.Equal(BookingErrors.OutsideSchedule, result.Error);
        Assert.Empty(_studies.All);
    }

    [Fact]
    public async Task BookingWithAnUnknownDoctorFails()
    {
        var unknownDoctorId = Guid.NewGuid();

        var result = await Book(unknownDoctorId, OfferedSlot);

        Assert.Equal(BookingErrors.DoctorNotFound(unknownDoctorId), result.Error);
        Assert.Empty(_studies.All);
    }

    private Task<Result<BookedStudy>> Book(Guid doctorId, DateTimeOffset start) =>
        _application.Send(new BookCheckupCommand(Patient.UserId, doctorId, start));

    private sealed class FakeIdentityDirectory(PatientSnapshot patient, DoctorSnapshot doctor, DateTimeOffset offeredSlot)
        : IIdentityDirectory
    {
        public Task<PatientSnapshot?> FindPatientAsync(Guid patientUserId, CancellationToken cancellationToken) =>
            Task.FromResult(patientUserId == patient.UserId ? patient : null);

        public Task<DoctorSnapshot?> FindDoctorAsync(Guid doctorId, CancellationToken cancellationToken) =>
            Task.FromResult(doctorId == doctor.Id ? doctor : null);

        public Task<bool> OffersSlotAsync(Guid doctorId, DateTimeOffset start, CancellationToken cancellationToken) =>
            Task.FromResult(doctorId == doctor.Id && start == offeredSlot);
    }
}
