using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Auth;
using MediView.Identity.Domain.Doctors;
using MediView.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MediView.Identity.Application.Doctors;

public sealed record WeeklyShift(DayOfWeek DayOfWeek, TimeOnly Start, TimeOnly End, int SlotMinutes);

public sealed record CreateDoctorCommand(
    string FullName,
    string Email,
    string Password,
    string LicenseNumber,
    string Specialty,
    IReadOnlyList<WeeklyShift> Shifts) : ICommand<Result<DoctorSummary>>;

internal sealed class CreateDoctorCommandHandler(
    IUserRepository users,
    IDoctorRepository doctors,
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher) : ICommandHandler<CreateDoctorCommand, Result<DoctorSummary>>
{
    public async Task<Result<DoctorSummary>> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        if (await users.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Result.Failure<DoctorSummary>(AuthErrors.EmailTaken);
        }

        if (await doctors.LicenseExistsAsync(request.LicenseNumber, cancellationToken))
        {
            return Result.Failure<DoctorSummary>(DoctorErrors.LicenseTaken);
        }

        var user = User.Create(
            request.Email,
            request.FullName,
            Role.Doctor,
            unhashed => passwordHasher.HashPassword(unhashed, request.Password));
        var doctor = Doctor.Create(user.Id, request.LicenseNumber, request.Specialty);

        foreach (var shift in request.Shifts)
        {
            doctor.AddSchedule(shift.DayOfWeek, shift.Start, shift.End, shift.SlotMinutes);
        }

        users.Add(user);
        doctors.Add(doctor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DoctorSummary(doctor.Id, user.FullName, doctor.Specialty);
    }
}
