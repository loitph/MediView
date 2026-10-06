using MediView.Identity.Domain.Users;

namespace MediView.Identity.Application.Auth;

public interface IAccessTokenIssuer
{
    public AccessToken Issue(User user, Guid? doctorId);
}
