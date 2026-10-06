using MediView.Reporting.Application.Reports;
using MediView.Reporting.Domain.Reports;
using Microsoft.EntityFrameworkCore;

namespace MediView.Reporting.Infrastructure.Persistence.Repositories;

internal sealed class ReportRepository(ReportingDbContext db) : IReportRepository
{
    public Task<Report?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Reports.SingleOrDefaultAsync(report => report.Id == id, cancellationToken);

    public Task<Report?> FindDraftAsync(Guid studyId, Guid doctorId, CancellationToken cancellationToken) =>
        db.Reports.SingleOrDefaultAsync(
            report => report.StudyId == studyId && report.DoctorId == doctorId && report.Status == ReportStatus.Draft,
            cancellationToken);

    public async Task<IReadOnlyList<ReportView>> ListAsync(Guid studyId, Guid? draftsOfDoctorId, CancellationToken cancellationToken) =>
        await db.Reports
            .AsNoTracking()
            .Where(report => report.StudyId == studyId)
            .Where(report => report.Status == ReportStatus.Finalized || report.DoctorId == draftsOfDoctorId)
            .OrderByDescending(report => report.Id)
            .Select(report => new ReportView(
                report.Id,
                report.StudyId,
                report.DoctorName,
                report.Status,
                report.Findings,
                report.Impression,
                report.Decision,
                report.FinalizedAt,
                report.Medications
                    .OrderBy(medication => medication.Id)
                    .Select(medication => new MedicationView(
                        medication.Id,
                        medication.DrugName,
                        medication.Dosage,
                        medication.Frequency,
                        medication.Duration))
                    .ToList()))
            .ToListAsync(cancellationToken);

    public void Add(Report report) => db.Reports.Add(report);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ReportErrors.ChangedConcurrently();
        }
    }
}
