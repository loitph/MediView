using MediView.BuildingBlocks.Application;

namespace MediView.Identity.Application.Doctors;

public sealed record DoctorSummary(Guid Id, string FullName, string Specialty);

public sealed record ListDoctorsQuery : IQuery<IReadOnlyList<DoctorSummary>>;

public sealed record GetDoctorQuery(Guid DoctorId) : IQuery<Result<DoctorSummary>>;

internal sealed class ListDoctorsQueryHandler(IDoctorRepository doctors)
    : IQueryHandler<ListDoctorsQuery, IReadOnlyList<DoctorSummary>>
{
    public Task<IReadOnlyList<DoctorSummary>> Handle(ListDoctorsQuery request, CancellationToken cancellationToken) =>
        doctors.ListAsync(cancellationToken);
}

internal sealed class GetDoctorQueryHandler(IDoctorRepository doctors)
    : IQueryHandler<GetDoctorQuery, Result<DoctorSummary>>
{
    public async Task<Result<DoctorSummary>> Handle(GetDoctorQuery request, CancellationToken cancellationToken) =>
        await doctors.FindSummaryAsync(request.DoctorId, cancellationToken) is { } doctor
            ? doctor
            : Result.Failure<DoctorSummary>(DoctorErrors.NotFound(request.DoctorId));
}
