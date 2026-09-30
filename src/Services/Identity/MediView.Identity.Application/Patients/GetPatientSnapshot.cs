using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Patients;

public sealed record PatientSnapshot(Guid UserId, string FullName, string Mrn);

public sealed record GetPatientSnapshotQuery(Guid UserId) : IQuery<Result<PatientSnapshot>>;

public static class PatientErrors
{
    public static Error NotFound(Guid userId) => new("patient.not_found", $"User {userId} is not a patient.");
}

internal sealed class GetPatientSnapshotQueryHandler(IPatientRepository patients)
    : IQueryHandler<GetPatientSnapshotQuery, Result<PatientSnapshot>>
{
    public async Task<Result<PatientSnapshot>> Handle(GetPatientSnapshotQuery request, CancellationToken cancellationToken) =>
        await patients.FindSnapshotAsync(request.UserId, cancellationToken) is { } snapshot
            ? snapshot
            : Result.Failure<PatientSnapshot>(PatientErrors.NotFound(request.UserId));
}
