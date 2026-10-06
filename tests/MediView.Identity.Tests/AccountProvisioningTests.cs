using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Auth;
using MediView.Identity.Application.Doctors;
using MediView.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace MediView.Identity.Tests;

public sealed class AccountProvisioningTests : IAsyncDisposable
{
    private const string Password = "correct-horse-battery";

    private static readonly WeeklyShift MondayMorning = new(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0), 30);

    private readonly InMemoryIdentityStore _store = new();
    private readonly IdentityApplication _application;

    public AccountProvisioningTests() => _application = new IdentityApplication(_store);

    public ValueTask DisposeAsync() => _application.DisposeAsync();

    [Fact]
    public async Task RegistrationSavesUserAndPatientInOneSaveChanges()
    {
        var result = await Register("pat@example.com");

        var user = Assert.Single(_store.Users);
        var patient = Assert.Single(_store.Patients);
        Assert.Equal(new RegisteredPatient(user.Id, patient.Id), result.Value);
        Assert.Equal(Role.Patient, user.Role);
        Assert.Equal(user.Id, patient.UserId);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task RegisteredPatientCanSignIn()
    {
        await Register("pat@example.com");

        var hasher = new PasswordHasher<User>();
        var user = Assert.Single(_store.Users);

        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, Password));
    }

    [Fact]
    public async Task RegisteringTwiceWithOneEmailFailsWithEmailTaken()
    {
        await Register("pat@example.com");

        var second = await Register("  PAT@Example.com ");

        Assert.Equal(AuthErrors.EmailTaken, second.Error);
        Assert.Single(_store.Users);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task CreatedDoctorSavesUserDoctorAndScheduleTogetherAndIsListed()
    {
        var result = await CreateDoctor("doc@example.com", "LIC-42");

        var user = Assert.Single(_store.Users);
        var doctor = Assert.Single(_store.Doctors);
        Assert.Equal(Role.Doctor, user.Role);
        Assert.Equal(user.Id, doctor.UserId);
        Assert.Single(doctor.Schedules);
        Assert.Equal(1, _store.SaveCount);
        Assert.Equal([result.Value], await _application.Send(new ListDoctorsQuery()));
    }

    [Fact]
    public async Task DoctorWithATakenEmailIsRejected()
    {
        await Register("taken@example.com");

        var result = await CreateDoctor("taken@example.com", "LIC-42");

        Assert.Equal(AuthErrors.EmailTaken, result.Error);
        Assert.Empty(_store.Doctors);
    }

    [Fact]
    public async Task DoctorWithATakenLicenceIsRejected()
    {
        await CreateDoctor("first@example.com", "LIC-42");

        var result = await CreateDoctor("second@example.com", " LIC-42 ");

        Assert.Equal(DoctorErrors.LicenseTaken, result.Error);
        Assert.Single(_store.Doctors);
    }

    private Task<Result<RegisteredPatient>> Register(string email) =>
        _application.Send(new RegisterPatientCommand(email, Password, "Pat Patient"));

    private Task<Result<DoctorSummary>> CreateDoctor(string email, string licenseNumber) =>
        _application.Send(new CreateDoctorCommand("Dora Doctor", email, Password, licenseNumber, "Radiology", [MondayMorning]));
}
