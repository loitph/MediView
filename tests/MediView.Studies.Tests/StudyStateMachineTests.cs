using MediView.BuildingBlocks.Domain;
using MediView.Studies.Domain.Studies;
using Xunit;

namespace MediView.Studies.Tests;

public sealed class StudyStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    [Fact]
    public void BookCreatesTodoStudyWithFirstHistoryRow()
    {
        var study = BookStudy();

        Assert.Equal(StudyStatus.ToDo, study.Status);
        var entry = Assert.Single(study.History);
        Assert.Null(entry.FromStatus);
        Assert.Equal(StudyStatus.ToDo, entry.ToStatus);
        Assert.Equal(PatientId, entry.ChangedBy);
    }

    [Fact]
    public void MarkImagesImportedKeepsStudyTodo()
    {
        var study = BookStudy();

        study.MarkImagesImported(Now);

        Assert.Equal(StudyStatus.ToDo, study.Status);
        Assert.Equal(Now, study.ImagesImportedAt);
        Assert.Single(study.History);
    }

    [Fact]
    public void StartReadMovesTodoToInProgress()
    {
        var study = ImportedStudy();

        study.StartRead(DoctorId, Now);

        AssertLastTransition(study, StudyStatus.ToDo, StudyStatus.InProgress, DoctorId);
    }

    [Fact]
    public void CompleteMovesInProgressToDone()
    {
        var study = StudyInProgress();

        study.Complete(Now);

        AssertLastTransition(study, StudyStatus.InProgress, StudyStatus.Done, DoctorId);
    }

    [Fact]
    public void RequestReDiagnosisMovesInProgressToReDiagnosis()
    {
        var study = StudyInProgress();

        study.RequestReDiagnosis(Now);

        AssertLastTransition(study, StudyStatus.InProgress, StudyStatus.ReDiagnosis, DoctorId);
    }

    [Fact]
    public void StartReadMovesReDiagnosisBackToInProgress()
    {
        var study = StudyInProgress();
        study.RequestReDiagnosis(Now);

        study.StartRead(DoctorId, Now);

        AssertLastTransition(study, StudyStatus.ReDiagnosis, StudyStatus.InProgress, DoctorId);
    }

    [Fact]
    public void CompleteOnTodoStudyThrows()
    {
        var study = BookStudy();

        Assert.Throws<DomainException>(() => study.Complete(Now));
        AssertUntouched(study, StudyStatus.ToDo);
    }

    [Fact]
    public void RequestReDiagnosisOnTodoStudyThrows()
    {
        var study = BookStudy();

        Assert.Throws<DomainException>(() => study.RequestReDiagnosis(Now));
        AssertUntouched(study, StudyStatus.ToDo);
    }

    [Fact]
    public void StartReadOnStudyAlreadyInProgressThrows()
    {
        var study = StudyInProgress();

        Assert.Throws<DomainException>(() => study.StartRead(DoctorId, Now));
        Assert.Equal(StudyStatus.InProgress, study.Status);
    }

    [Fact]
    public void CompleteOnDoneStudyThrows()
    {
        var study = StudyInProgress();
        study.Complete(Now);

        Assert.Throws<DomainException>(() => study.Complete(Now));
        Assert.Equal(StudyStatus.Done, study.Status);
    }

    [Fact]
    public void StartReadWithoutImagesThrows()
    {
        var study = BookStudy();

        Assert.Throws<DomainException>(() => study.StartRead(DoctorId, Now));
        AssertUntouched(study, StudyStatus.ToDo);
    }

    [Fact]
    public void StartReadByUnassignedDoctorThrows()
    {
        var study = ImportedStudy();

        Assert.Throws<DomainException>(() => study.StartRead(Guid.NewGuid(), Now));
        AssertUntouched(study, StudyStatus.ToDo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AdminOverrideWithoutReasonThrows(string reason)
    {
        var study = BookStudy();

        Assert.Throws<DomainException>(() => study.AdminOverride(StudyStatus.Done, AdminId, reason, Now));
        AssertUntouched(study, StudyStatus.ToDo);
    }

    [Fact]
    public void AdminOverrideToCurrentStatusThrows()
    {
        var study = BookStudy();

        Assert.Throws<DomainException>(() => study.AdminOverride(StudyStatus.ToDo, AdminId, "Doctor on leave", Now));
        AssertUntouched(study, StudyStatus.ToDo);
    }

    [Fact]
    public void AdminOverrideBypassesTransitionTableAndRecordsActorAndReason()
    {
        var study = BookStudy();

        study.AdminOverride(StudyStatus.Done, AdminId, "Doctor on leave", Now);

        var entry = AssertLastTransition(study, StudyStatus.ToDo, StudyStatus.Done, AdminId);
        Assert.Equal("Doctor on leave", entry.Reason);
    }

    private static Study BookStudy() =>
        Study.Book(PatientId, "Test Patient", "MRN-0001", DoctorId, "Test Doctor", Now.AddDays(1), StudyPriority.Routine, Now);

    private static Study ImportedStudy()
    {
        var study = BookStudy();
        study.MarkImagesImported(Now);
        return study;
    }

    private static Study StudyInProgress()
    {
        var study = ImportedStudy();
        study.StartRead(DoctorId, Now);
        return study;
    }

    private static StudyStatusHistory AssertLastTransition(Study study, StudyStatus from, StudyStatus to, Guid changedBy)
    {
        Assert.Equal(to, study.Status);
        var entry = study.History.Last();
        Assert.Equal(from, entry.FromStatus);
        Assert.Equal(to, entry.ToStatus);
        Assert.Equal(changedBy, entry.ChangedBy);
        return entry;
    }

    private static void AssertUntouched(Study study, StudyStatus status)
    {
        Assert.Equal(status, study.Status);
        Assert.Single(study.History);
    }
}
