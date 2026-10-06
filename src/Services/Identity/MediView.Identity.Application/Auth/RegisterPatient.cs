using MediView.BuildingBlocks.Application;
using MediView.Identity.Application.Patients;
using MediView.Identity.Domain.Patients;
using MediView.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MediView.Identity.Application.Auth;

public sealed record RegisterPatientCommand(string Email, string Password, string FullName)
    : ICommand<Result<RegisteredPatient>>;

public sealed record RegisteredPatient(Guid UserId, Guid PatientId);

internal sealed class RegisterPatientCommandHandler(
    IUserRepository users,
    IPatientRepository patients,
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher) : ICommandHandler<RegisterPatientCommand, Result<RegisteredPatient>>
{
    public async Task<Result<RegisteredPatient>> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        if (await users.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Result.Failure<RegisteredPatient>(AuthErrors.EmailTaken);
        }

        var user = User.Create(
            request.Email,
            request.FullName,
            Role.Patient,
            unhashed => passwordHasher.HashPassword(unhashed, request.Password));
        var patient = Patient.Create(user.Id);

        users.Add(user);
        patients.Add(patient);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisteredPatient(user.Id, patient.Id);
    }
}
