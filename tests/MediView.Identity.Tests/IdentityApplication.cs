using MediView.BuildingBlocks.Application;
using MediView.Identity.Application;
using MediView.Identity.Application.Auth;
using MediView.Identity.Application.Doctors;
using MediView.Identity.Application.Patients;
using MediView.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediView.Identity.Tests;

internal sealed class IdentityApplication(InMemoryIdentityStore store, TimeProvider timeProvider) : IAsyncDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddIdentityApplication()
        .AddSingleton<IUserRepository>(store)
        .AddSingleton<IPatientRepository>(store)
        .AddSingleton<IDoctorRepository>(store)
        .AddSingleton<IUnitOfWork>(store)
        .AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()
        .AddSingleton(timeProvider)
        .BuildServiceProvider();

    public IdentityApplication(InMemoryIdentityStore store)
        : this(store, TimeProvider.System)
    {
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(request, TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
