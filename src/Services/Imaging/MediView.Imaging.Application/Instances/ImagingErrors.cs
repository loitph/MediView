using MediView.BuildingBlocks.Application;
using MediView.BuildingBlocks.Domain;

namespace MediView.Imaging.Application.Instances;

public static class ImagingErrors
{
    public static readonly Error NoFiles = new("imaging.no_files", "Attach at least one DICOM file.");

    public static readonly Error StudyNotUpdated = new(
        "imaging.study_not_updated",
        "The images are stored, but the study could not be marked as imported. Upload the same files again to retry.");

    public static Error StudyNotFound(Guid studyId) => new("imaging.study_not_found", $"Study {studyId} does not exist.");

    public static Error NotDicom(string fileName) =>
        new("imaging.not_dicom", $"'{fileName}' is not a DICOM file. Nothing was imported.");

    public static ConflictException ImportInProgress() =>
        new("Another import of the same images finished first. Upload again to see the result.");
}
