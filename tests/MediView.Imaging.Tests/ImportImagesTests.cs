using MediView.BuildingBlocks.Application;
using MediView.Imaging.Application.Instances;
using Xunit;

namespace MediView.Imaging.Tests;

public sealed class ImportImagesTests : IAsyncDisposable
{
    private static readonly Guid StudyId = Guid.NewGuid();

    private readonly FakeStudies _studies = new(StudyId);
    private readonly InMemoryBlobStore _blobs = new();
    private readonly InMemoryInstanceRepository _instances = new();
    private readonly ImagingApplication _application;

    public ImportImagesTests() =>
        _application = new ImagingApplication(_studies, new TextDicomReader(), _blobs, _instances);

    public ValueTask DisposeAsync() => _application.DisposeAsync();

    [Fact]
    public async Task ImportStoresEachFileSavesItsInstanceAndMarksTheStudy()
    {
        var result = await Import(StudyId, TextDicomReader.Dicom("1.1", 1), TextDicomReader.Dicom("1.2", 2));

        Assert.Equal(new ImportedImages(2, 0), result.Value);
        Assert.Equal(["1.1", "1.2"], _instances.Saved.Select(instance => instance.SopInstanceUid));
        Assert.Equal(_instances.Saved.Select(instance => instance.StoragePath).Order(), _blobs.Keys.Order());
        Assert.Equal(1, _studies.MarkedCount);
    }

    [Fact]
    public async Task ANonDicomFileRejectsTheWholeBatchAndStoresNothing()
    {
        var result = await Import(StudyId, TextDicomReader.Dicom("1.1"), TextDicomReader.Png("chest.png"));

        Assert.Equal(ImagingErrors.NotDicom("chest.png"), result.Error);
        Assert.Empty(_blobs.Keys);
        Assert.Empty(_instances.Saved);
        Assert.Equal(0, _studies.MarkedCount);
    }

    [Fact]
    public async Task ReimportSkipsInstancesTheStudyAlreadyHasAndMarksTheStudyAgain()
    {
        await Import(StudyId, TextDicomReader.Dicom("1.1"));

        var result = await Import(StudyId, TextDicomReader.Dicom("1.1"), TextDicomReader.Dicom("1.2"));

        Assert.Equal(new ImportedImages(1, 1), result.Value);
        Assert.Equal(2, _instances.Saved.Count);
        Assert.Equal(2, _blobs.Keys.Count);
        Assert.Equal(2, _studies.MarkedCount);
    }

    [Fact]
    public async Task AFailedSaveDeletesTheFilesItStored()
    {
        _instances.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Import(StudyId, TextDicomReader.Dicom("1.1"), TextDicomReader.Dicom("1.2")));

        Assert.Empty(_blobs.Keys);
        Assert.Equal(0, _studies.MarkedCount);
    }

    [Fact]
    public async Task WhenStudiesCannotBeToldTheImagesStayAndTheUploadCanBeRetried()
    {
        _studies.StudiesServiceUp = false;

        var failed = await Import(StudyId, TextDicomReader.Dicom("1.1"));

        Assert.Equal(ImagingErrors.StudyNotUpdated, failed.Error);
        Assert.Single(_instances.Saved);

        _studies.StudiesServiceUp = true;
        var retried = await Import(StudyId, TextDicomReader.Dicom("1.1"));

        Assert.Equal(new ImportedImages(0, 1), retried.Value);
        Assert.Equal(1, _studies.MarkedCount);
    }

    [Fact]
    public async Task ImportIntoAnUnknownStudyFails()
    {
        var unknownStudyId = Guid.NewGuid();

        var result = await Import(unknownStudyId, TextDicomReader.Dicom("1.1"));

        Assert.Equal(ImagingErrors.StudyNotFound(unknownStudyId), result.Error);
        Assert.Empty(_blobs.Keys);
    }

    private Task<Result<ImportedImages>> Import(Guid studyId, params UploadedFile[] files) =>
        _application.Send(new ImportImagesCommand(studyId, files));
}
