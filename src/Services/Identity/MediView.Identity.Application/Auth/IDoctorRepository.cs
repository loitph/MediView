namespace MediView.Identity.Application.Auth;

public interface IDoctorRepository
{
    public Task<Guid?> FindIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
