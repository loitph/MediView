using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public interface IStudyReadStore
{
    public IQueryable<Study> Studies { get; }
}
