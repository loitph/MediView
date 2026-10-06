using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;
using MediView.Reporting.Application.Reports;
using MediView.Reporting.Domain.Reports;
using Xunit;

namespace MediView.Reporting.Tests;

public sealed class ReportWritingTests : IAsyncDisposable
{
    private static readonly Guid StudyId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();
    private static readonly Guid OtherDoctorId = Guid.NewGuid();

    private readonly FakeStudies _studies = new() { LockOwner = DoctorId };
    private readonly InMemoryReportRepository _reports = new();
    private readonly ReportingApplication _application;

    public ReportWritingTests() => _application = new ReportingApplication(_studies, _reports);

    public ValueTask DisposeAsync() => _application.DisposeAsync();

    [Fact]
    public async Task StartingTwiceReturnsTheSameDraft()
    {
        var first = await Start(DoctorId);
        var second = await Start(DoctorId);

        Assert.Equal(first, second);
        Assert.Single(_reports.All);
    }

    [Fact]
    public async Task ADoctorWithoutTheLockCannotStartAReport()
    {
        await Assert.ThrowsAsync<ConflictException>(() => Start(OtherDoctorId));

        Assert.Empty(_reports.All);
    }

    [Fact]
    public async Task ADoctorWhoLostTheLockGetsConflictOnSave()
    {
        var reportId = await Start(DoctorId);
        _studies.LockOwner = OtherDoctorId;

        await Assert.ThrowsAsync<ConflictException>(() =>
            _application.Send(new UpdateReportTextCommand(reportId, DoctorId, "Clear lungs.", null)));

        Assert.Null(_reports.All.Single().Findings);
    }

    [Fact]
    public async Task AnotherDoctorHoldingTheLockStillCannotEditTheDraft()
    {
        var reportId = await Start(DoctorId);
        _studies.LockOwner = OtherDoctorId;

        await Assert.ThrowsAsync<ConflictException>(() =>
            _application.Send(new UpdateReportTextCommand(reportId, OtherDoctorId, "Overwritten.", null)));
    }

    [Fact]
    public async Task MedicationsAreAddedAndRemovedUnderTheLock()
    {
        var reportId = await Start(DoctorId);

        var added = await _application.Send(new AddMedicationCommand(reportId, DoctorId, "Amoxicillin", "500 mg", "3 times a day", "7 days"));
        Assert.Single(_reports.All.Single().Medications);

        await _application.Send(new RemoveMedicationCommand(reportId, DoctorId, added.Value));
        Assert.Empty(_reports.All.Single().Medications);
    }

    [Fact]
    public async Task FinalizeFreezesTheReportAndFinishesTheRead()
    {
        var reportId = await WrittenReport();

        var result = await Finalize(reportId, ReportDecision.Complete);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportStatus.Finalized, _reports.All.Single().Status);
        Assert.Equal([ReportDecision.Complete], _studies.FinishedReads);
    }

    [Fact]
    public async Task WhenStudiesIsDownTheReportStaysFinalizedAndFinalizeCanBeRetried()
    {
        var reportId = await WrittenReport();
        _studies.StudiesServiceUp = false;

        var failed = await Finalize(reportId, ReportDecision.Complete);

        Assert.Equal(ReportErrors.StudyNotUpdated, failed.Error);
        Assert.Equal(ReportStatus.Finalized, _reports.All.Single().Status);

        _studies.StudiesServiceUp = true;
        _studies.LockOwner = null;
        var retried = await Finalize(reportId, ReportDecision.Complete);

        Assert.True(retried.IsSuccess);
        Assert.Equal([ReportDecision.Complete], _studies.FinishedReads);
    }

    [Fact]
    public async Task ARetryCannotChangeTheDecision()
    {
        var reportId = await WrittenReport();
        await Finalize(reportId, ReportDecision.Complete);

        await Assert.ThrowsAsync<DomainException>(() => Finalize(reportId, ReportDecision.ReDiagnosis));
    }

    [Fact]
    public async Task ReDiagnosisLeadsToASecondReportNeverAnEdit()
    {
        var first = await WrittenReport();
        await Finalize(first, ReportDecision.ReDiagnosis);

        var second = await Start(DoctorId);

        Assert.NotEqual(first, second);
        Assert.Equal(2, _reports.All.Count);
        Assert.Equal(ReportStatus.Finalized, _reports.All.Single(report => report.Id == first).Status);
    }

    [Fact]
    public async Task PatientsSeeOnlyFinalizedReports()
    {
        var finalized = await WrittenReport();
        await Finalize(finalized, ReportDecision.ReDiagnosis);
        await Start(DoctorId);

        var listed = await _application.Send(new ListReportsQuery(StudyId, null));

        Assert.Equal(finalized, Assert.Single(listed.Value).Id);
    }

    [Fact]
    public async Task ReportsOfAStudyTheCallerCannotReadAreHidden()
    {
        _studies.Readable = false;

        var listed = await _application.Send(new ListReportsQuery(StudyId, null));

        Assert.Equal(ReportErrors.StudyNotFound(StudyId), listed.Error);
    }

    private Task<Guid> Start(Guid doctorId) =>
        _application.Send(new StartReportCommand(StudyId, doctorId, "Dr. B"));

    private async Task<Guid> WrittenReport()
    {
        var reportId = await Start(DoctorId);
        await _application.Send(new UpdateReportTextCommand(reportId, DoctorId, "Clear lungs.", "No acute findings."));
        return reportId;
    }

    private Task<Result> Finalize(Guid reportId, ReportDecision decision) =>
        _application.Send(new FinalizeReportCommand(reportId, DoctorId, decision));
}
