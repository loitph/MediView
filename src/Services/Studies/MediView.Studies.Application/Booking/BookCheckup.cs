using MediView.BuildingBlocks.Application;
using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Booking;

public sealed record BookCheckupCommand(Guid PatientUserId, Guid DoctorId, DateTimeOffset ScheduledStart)
    : ICommand<Result<BookedStudy>>;

public sealed record BookedStudy(Guid Id, string StudyNumber);

internal sealed class BookCheckupCommandHandler(
    IIdentityDirectory identity,
    IStudyRepository studies,
    TimeProvider timeProvider) : ICommandHandler<BookCheckupCommand, Result<BookedStudy>>
{
    public async Task<Result<BookedStudy>> Handle(BookCheckupCommand request, CancellationToken cancellationToken)
    {
        var start = request.ScheduledStart.ToUniversalTime();

        var patient = await identity.FindPatientAsync(request.PatientUserId, cancellationToken);
        if (patient is null)
        {
            return Result.Failure<BookedStudy>(BookingErrors.PatientNotFound(request.PatientUserId));
        }

        var doctor = await identity.FindDoctorAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure<BookedStudy>(BookingErrors.DoctorNotFound(request.DoctorId));
        }

        if (!await identity.OffersSlotAsync(doctor.Id, start, cancellationToken))
        {
            return Result.Failure<BookedStudy>(BookingErrors.OutsideSchedule);
        }

        if (await studies.IsSlotTakenAsync(doctor.Id, start, cancellationToken))
        {
            throw BookingErrors.SlotTaken();
        }

        var study = Study.Book(
            patient.UserId,
            patient.FullName,
            patient.Mrn,
            doctor.Id,
            doctor.FullName,
            start,
            StudyPriority.Routine,
            timeProvider.GetUtcNow());

        studies.Add(study);
        await studies.SaveChangesAsync(cancellationToken);

        return new BookedStudy(study.Id, study.StudyNumber);
    }
}
