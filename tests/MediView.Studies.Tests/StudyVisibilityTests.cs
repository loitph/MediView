using MediView.Studies.Application.Studies;
using MediView.Studies.Domain.Studies;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class StudyVisibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid OtherPatientId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();
    private static readonly Guid OtherDoctorId = Guid.NewGuid();

    private readonly Study _routineEarly = Book(PatientId, DoctorId, Now.AddHours(1), StudyPriority.Routine);
    private readonly Study _urgentLate = Book(OtherPatientId, DoctorId, Now.AddHours(5), StudyPriority.Urgent);
    private readonly Study _statLatest = Book(PatientId, DoctorId, Now.AddHours(9), StudyPriority.Stat);
    private readonly Study _otherDoctors = Book(PatientId, OtherDoctorId, Now.AddHours(2), StudyPriority.Stat);

    private IQueryable<Study> All => new[] { _routineEarly, _urgentLate, _statLatest, _otherDoctors }.AsQueryable();

    [Fact]
    public void DoctorSeesOnlyTheirOwnStudiesStatFirst()
    {
        var worklist = new DoctorViewer(DoctorId).Visible(All).InWorklistOrder().ToList();

        Assert.Equal([_statLatest, _urgentLate, _routineEarly], worklist);
    }

    [Fact]
    public void PatientSeesOnlyTheirOwnStudies()
    {
        var studies = new PatientViewer(PatientId).Visible(All).ToList();

        Assert.DoesNotContain(_urgentLate, studies);
        Assert.Equal(3, studies.Count);
    }

    [Fact]
    public void AdminSeesEveryStudy()
    {
        Assert.Equal(4, new AdminViewer().Visible(All).Count());
    }

    [Fact]
    public void StudiesOfEqualPriorityAreOrderedByScheduledStart()
    {
        var worklist = new PatientViewer(PatientId).Visible(All).InWorklistOrder().ToList();

        Assert.Equal([_otherDoctors, _statLatest, _routineEarly], worklist);
    }

    private static Study Book(Guid patientId, Guid doctorId, DateTimeOffset start, StudyPriority priority) =>
        Study.Book(patientId, "Patient A", "MRN-1042", doctorId, "Dr. B", start, priority, Now);
}
