using MediView.Identity.Api.Data;
using MediView.Identity.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediView.Identity.Api.Endpoints;

public static class AuthEndpoints
{
    public record LoginRequest(string Email, string Password);

    public record RegisterRequest(string Email, string Password, string FullName);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", Login);
        app.MapPost("/auth/register", Register);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IdentityDbContext db,
        PasswordHasher<User> hasher,
        TokenService tokens)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var check = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        var doctor = await db.Doctors.FirstOrDefaultAsync(d => d.UserId == user.Id);
        var token = tokens.CreateToken(user, doctor?.Id);
        return Results.Ok(new { Token = token });
    }

    private static async Task<IResult> Register(RegisterRequest request, IdentityDbContext db, PasswordHasher<User> hasher)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return Results.Problem("An account with this email already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        var user = new User { Id = Guid.CreateVersion7(), Email = email, FullName = request.FullName.Trim(), Role = Role.Patient };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Patients.Add(new Patient { Id = Guid.CreateVersion7(), User = user });
        await db.SaveChangesAsync();

        return Results.Ok();
    }
}
