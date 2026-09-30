namespace MediView.Identity.Domain.Doctors;

public readonly record struct ScheduledSlot(DateOnly Date, TimeOnly Start, TimeOnly End);
