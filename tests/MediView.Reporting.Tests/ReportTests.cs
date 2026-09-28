using MediView.BuildingBlocks.Domain;
using MediView.Reporting.Domain.Reports;
using Xunit;

namespace MediView.Reporting.Tests;

public sealed class ReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DraftStartsEmptyAndEditable()
    {
        var report = NewDraft();

        Assert.Equal(ReportStatus.Draft, report.Status);
        Assert.Null(report.Findings);
        Assert.Null(report.Impression);
        Assert.Empty(report.Medications);
    }

    [Fact]
    public void UpdateTextStoresTrimmedTextAndBlankAsNull()
    {
        var report = NewDraft();

        report.UpdateText("  Clear lungs.  ", "   ");

        Assert.Equal("Clear lungs.", report.Findings);
        Assert.Null(report.Impression);
    }

    [Fact]
    public void AddAndRemoveMedicationOnDraft()
    {
        var report = NewDraft();

        var medication = report.AddMedication("Amoxicillin", "500 mg", "3 times a day", "7 days");
        Assert.Same(medication, Assert.Single(report.Medications));

        report.RemoveMedication(medication.Id);
        Assert.Empty(report.Medications);
    }

    [Fact]
    public void AddMedicationWithoutDrugNameThrows()
    {
        var report = NewDraft();

        Assert.Throws<DomainException>(() => report.AddMedication(" ", "500 mg", "3 times a day", "7 days"));
        Assert.Empty(report.Medications);
    }

    [Fact]
    public void RemovingUnknownMedicationThrows()
    {
        var report = NewDraft();

        Assert.Throws<DomainException>(() => report.RemoveMedication(Guid.NewGuid()));
    }

    [Fact]
    public void FinalizeFreezesDecisionAndTime()
    {
        var report = NewDraft();
        report.UpdateText("Clear lungs.", "No acute findings.");

        report.Finalize(ReportDecision.Complete, Now);

        Assert.Equal(ReportStatus.Finalized, report.Status);
        Assert.Equal(ReportDecision.Complete, report.Decision);
        Assert.Equal(Now, report.FinalizedAt);
    }

    [Fact]
    public void FinalizingWithoutImpressionThrows()
    {
        var report = NewDraft();
        report.UpdateText("Clear lungs.", null);

        Assert.Throws<DomainException>(() => report.Finalize(ReportDecision.Complete, Now));
        Assert.Equal(ReportStatus.Draft, report.Status);
    }

    [Fact]
    public void FinalizingWithoutFindingsThrows()
    {
        var report = NewDraft();
        report.UpdateText(null, "No acute findings.");

        Assert.Throws<DomainException>(() => report.Finalize(ReportDecision.Complete, Now));
        Assert.Equal(ReportStatus.Draft, report.Status);
    }

    [Fact]
    public void FinalizingTwiceThrows()
    {
        var report = FinalizedReport();

        Assert.Throws<DomainException>(() => report.Finalize(ReportDecision.ReDiagnosis, Now));
        Assert.Equal(ReportDecision.Complete, report.Decision);
    }

    [Fact]
    public void UpdateTextOnFinalizedReportThrows()
    {
        var report = FinalizedReport();

        Assert.Throws<DomainException>(() => report.UpdateText("Changed.", "Changed."));
        Assert.Equal("Clear lungs.", report.Findings);
    }

    [Fact]
    public void AddMedicationOnFinalizedReportThrows()
    {
        var report = FinalizedReport();

        Assert.Throws<DomainException>(() => report.AddMedication("Ibuprofen", "200 mg", "as needed", "3 days"));
        Assert.Single(report.Medications);
    }

    [Fact]
    public void RemoveMedicationOnFinalizedReportThrows()
    {
        var report = FinalizedReport();
        var medication = Assert.Single(report.Medications);

        Assert.Throws<DomainException>(() => report.RemoveMedication(medication.Id));
        Assert.Single(report.Medications);
    }

    private static Report NewDraft() => Report.Draft(Guid.NewGuid(), Guid.NewGuid(), "Test Doctor");

    private static Report FinalizedReport()
    {
        var report = NewDraft();
        report.UpdateText("Clear lungs.", "No acute findings.");
        report.AddMedication("Amoxicillin", "500 mg", "3 times a day", "7 days");
        report.Finalize(ReportDecision.Complete, Now);
        return report;
    }
}
