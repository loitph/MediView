using MediView.BuildingBlocks.Application;

namespace MediView.Reporting.Application.Reports;

public sealed record UpdateReportTextCommand(Guid ReportId, Guid DoctorId, string? Findings, string? Impression) : ICommand<Result>;

public sealed record AddMedicationCommand(
    Guid ReportId,
    Guid DoctorId,
    string DrugName,
    string Dosage,
    string Frequency,
    string Duration) : ICommand<Result<Guid>>;

public sealed record RemoveMedicationCommand(Guid ReportId, Guid DoctorId, Guid MedicationId) : ICommand<Result>;

internal sealed class UpdateReportTextCommandHandler(IReportRepository reports, ReportWriter writer)
    : ICommandHandler<UpdateReportTextCommand, Result>
{
    public async Task<Result> Handle(UpdateReportTextCommand request, CancellationToken cancellationToken)
    {
        var opened = await writer.OpenForWritingAsync(request.ReportId, request.DoctorId, cancellationToken);
        if (opened.IsFailure)
        {
            return opened;
        }

        opened.Value.UpdateText(request.Findings, request.Impression);
        await reports.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class AddMedicationCommandHandler(IReportRepository reports, ReportWriter writer)
    : ICommandHandler<AddMedicationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddMedicationCommand request, CancellationToken cancellationToken)
    {
        var opened = await writer.OpenForWritingAsync(request.ReportId, request.DoctorId, cancellationToken);
        if (opened.IsFailure)
        {
            return Result.Failure<Guid>(opened.Error);
        }

        var medication = opened.Value.AddMedication(request.DrugName, request.Dosage, request.Frequency, request.Duration);
        await reports.SaveChangesAsync(cancellationToken);
        return medication.Id;
    }
}

internal sealed class RemoveMedicationCommandHandler(IReportRepository reports, ReportWriter writer)
    : ICommandHandler<RemoveMedicationCommand, Result>
{
    public async Task<Result> Handle(RemoveMedicationCommand request, CancellationToken cancellationToken)
    {
        var opened = await writer.OpenForWritingAsync(request.ReportId, request.DoctorId, cancellationToken);
        if (opened.IsFailure)
        {
            return opened;
        }

        opened.Value.RemoveMedication(request.MedicationId);
        await reports.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
