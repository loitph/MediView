using MediView.Identity.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace MediView.Identity.Api.Data;

public static class SeedData
{
    public const string Password = "MediView#2026";

    public static void AddDemoAccounts(IdentityDbContext db, PasswordHasher<User> hasher)
    {
        if (db.Users.Any())
        {
            return;
        }

        db.Users.Add(NewUser("admin@mediview.local", "Ada Admin", Role.Admin, hasher));

        var doctor = new Doctor
        {
            Id = Guid.CreateVersion7(),
            User = NewUser("doctor@mediview.local", "Dan Doctor", Role.Doctor, hasher),
            LicenseNumber = "LIC-000001",
            Specialty = "Radiology",
        };

        DayOfWeek[] workingDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
        foreach (var day in workingDays)
        {
            doctor.Schedules.Add(new DoctorSchedule
            {
                Id = Guid.CreateVersion7(),
                DayOfWeek = day,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0),
                SlotMinutes = 30,
            });
        }

        db.Doctors.Add(doctor);

        db.Patients.Add(new Patient
        {
            Id = Guid.CreateVersion7(),
            User = NewUser("patient@mediview.local", "Pat Patient", Role.Patient, hasher),
        });

        db.SaveChanges();
    }

    private static User NewUser(string email, string fullName, Role role, PasswordHasher<User> hasher)
    {
        var user = new User { Id = Guid.CreateVersion7(), Email = email, FullName = fullName, Role = role };
        user.PasswordHash = hasher.HashPassword(user, Password);
        return user;
    }
}
