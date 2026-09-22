using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediView.BuildingBlocks.Api.Tests;

public sealed class AuthPoliciesTests
{
    private static readonly string DoctorId = Guid.NewGuid().ToString();

    private static IAuthorizationService CreateAuthorizationService() =>
        new ServiceCollection()
            .AddLogging()
            .AddMediViewAuth(new ConfigurationBuilder()
                .AddInMemoryCollection([new("Jwt:SigningKey", new string('k', JwtOptions.MinimumSigningKeyBytes))])
                .Build())
            .BuildServiceProvider()
            .GetRequiredService<IAuthorizationService>();

    private static ClaimsPrincipal UserInRole(string role, params Claim[] extraClaims) =>
        new(new ClaimsIdentity(
            [new Claim(MediViewClaims.Subject, Guid.NewGuid().ToString()), new Claim(MediViewClaims.Role, role), .. extraClaims],
            "Bearer",
            MediViewClaims.Name,
            MediViewClaims.Role));

    private static async Task<bool> IsAuthorized(ClaimsPrincipal user, string policy) =>
        (await CreateAuthorizationService().AuthorizeAsync(user, policy)).Succeeded;

    [Fact]
    public async Task DoctorWithDoctorIdSatisfiesDoctorOnly() =>
        Assert.True(await IsAuthorized(
            UserInRole(MediViewRoles.Doctor, new Claim(MediViewClaims.DoctorId, DoctorId)),
            AuthPolicies.DoctorOnly));

    [Fact]
    public async Task DoctorWithoutDoctorIdDoesNotSatisfyDoctorOnly() =>
        Assert.False(await IsAuthorized(UserInRole(MediViewRoles.Doctor), AuthPolicies.DoctorOnly));

    [Fact]
    public async Task AdminDoesNotSatisfyDoctorOnly() =>
        Assert.False(await IsAuthorized(UserInRole(MediViewRoles.Admin), AuthPolicies.DoctorOnly));

    [Theory]
    [InlineData(MediViewRoles.Patient, AuthPolicies.PatientOnly, true)]
    [InlineData(MediViewRoles.Admin, AuthPolicies.AdminOnly, true)]
    [InlineData(MediViewRoles.Patient, AuthPolicies.AdminOnly, false)]
    [InlineData(MediViewRoles.Admin, AuthPolicies.PatientOnly, false)]
    [InlineData(MediViewRoles.Doctor, AuthPolicies.PatientOnly, false)]
    [InlineData(MediViewRoles.Doctor, AuthPolicies.AdminOnly, false)]
    public async Task EachRoleSatisfiesOnlyItsOwnPolicy(string role, string policy, bool expected) =>
        Assert.Equal(expected, await IsAuthorized(UserInRole(role), policy));
}
