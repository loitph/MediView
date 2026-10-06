using MediView.BuildingBlocks.Application;

namespace MediView.Imaging.Application.Instances;

public sealed record ListInstancesQuery(Guid StudyId) : IQuery<Result<IReadOnlyList<InstanceSummary>>>;

internal sealed class ListInstancesQueryHandler(IStudiesClient studies, IInstanceRepository instances)
    : IQueryHandler<ListInstancesQuery, Result<IReadOnlyList<InstanceSummary>>>
{
    public async Task<Result<IReadOnlyList<InstanceSummary>>> Handle(ListInstancesQuery request, CancellationToken cancellationToken)
    {
        if (!await studies.CanReadAsync(request.StudyId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<InstanceSummary>>(ImagingErrors.StudyNotFound(request.StudyId));
        }

        return Result.Success(await instances.ListAsync(request.StudyId, cancellationToken));
    }
}
