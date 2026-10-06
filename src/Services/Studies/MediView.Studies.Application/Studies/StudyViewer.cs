using MediView.Studies.Domain.Studies;

namespace MediView.Studies.Application.Studies;

public abstract record StudyViewer
{
    public abstract IQueryable<Study> Listed(IQueryable<Study> studies);

    public virtual IQueryable<Study> Readable(IQueryable<Study> studies) => Listed(studies);
}

public sealed record PatientViewer(Guid PatientId) : StudyViewer
{
    public override IQueryable<Study> Listed(IQueryable<Study> studies) =>
        studies.Where(study => study.PatientId == PatientId);
}

public sealed record DoctorViewer(Guid DoctorId) : StudyViewer
{
    public override IQueryable<Study> Listed(IQueryable<Study> studies) =>
        studies.Where(study => study.DoctorId == DoctorId);

    public override IQueryable<Study> Readable(IQueryable<Study> studies) => studies;
}

public sealed record AdminViewer : StudyViewer
{
    public override IQueryable<Study> Listed(IQueryable<Study> studies) => studies;
}
