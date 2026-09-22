using MediView.Identity.Domain.Doctors;
using MediView.Identity.Domain.Patients;
using MediView.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace MediView.Identity.Infrastructure.Persistence.Seeding;

internal sealed class IdentitySeeder(IdentityDbContext db, IPasswordHasher<User> passwordHasher)
{
    public const string DevelopmentPassword = "MediView#2026";

    private static readonly DayOfWeek[] WorkingDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
    ];

    private static readonly TimeOnly ShiftStart = new(8, 0);
    private static readonly TimeOnly ShiftEnd = new(16, 0);
    private const int SlotMinutes = 30;

    public void Seed()
    {
        if (db.Users.Any())
        {
            return;
        }

        AddUser("admin@mediview.local", "Ada Admin", Role.Admin);
        AddDoctor(AddUser("doctor@mediview.local", "Dan Doctor", Role.Doctor));
        db.Patients.Add(Patient.Create(AddUser("patient@mediview.local", "Pat Patient", Role.Patient).Id));

        db.SaveChanges();
    }

    private User AddUser(string email, string fullName, Role role)
    {
        var unhashed = User.Create(email, string.Empty, fullName, role);
        var user = User.Create(email, passwordHasher.HashPassword(unhashed, DevelopmentPassword), fullName, role);
        db.Users.Add(user);
        return user;
    }

    private void AddDoctor(User user)
    {
        var doctor = Doctor.Create(user.Id, "LIC-000001", "Radiology");
        foreach (var day in WorkingDays)
        {
            doctor.AddSchedule(day, ShiftStart, ShiftEnd, SlotMinutes);
        }

        db.Doctors.Add(doctor);
    }
}
