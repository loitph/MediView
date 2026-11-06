using FellowOakDicom;
using MediView.Imaging.Api.Data;
using MediView.Imaging.Api.Entities;
using MediView.Shared;
using Microsoft.EntityFrameworkCore;

namespace MediView.Imaging.Api.Endpoints;

public static class InstanceEndpoints
{
    public const long MaxUploadBytes = 256L * 1024 * 1024;

    public record DicomUpload(IFormFile File, string SeriesInstanceUid, string SopInstanceUid, int InstanceNumber);

    public record ImportResult(int Imported, int AlreadyPresent);

    public record InstanceSummary(Guid Id, string SeriesInstanceUid);

    public static void MapInstanceEndpoints(this WebApplication app)
    {
        app.MapPost("/studies/{studyId:guid}/images", ImportImages)
            .RequireAuthorization(Roles.Admin)
            .DisableAntiforgery()
            .WithFormOptions(multipartBodyLengthLimit: MaxUploadBytes);

        app.MapGet("/studies/{studyId:guid}/instances", ListInstances)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Doctor, Roles.Admin));

        app.MapGet("/instances/{id:guid}/file", GetFile)
            .RequireAuthorization(policy => policy.RequireRole(Roles.Doctor, Roles.Admin));
    }

    private static async Task<IResult> ImportImages(
        Guid studyId,
        IFormFileCollection files,
        ImagingDbContext db,
        ImageStorage storage,
        StudiesClient studies)
    {
        if (files.Count == 0)
        {
            return Results.Problem("Attach at least one DICOM file.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!await studies.StudyExistsAsync(studyId))
        {
            return Results.Problem("This study does not exist.", statusCode: StatusCodes.Status404NotFound);
        }

        var uploads = new List<DicomUpload>();
        foreach (var file in files)
        {
            var upload = await ReadDicomAsync(file);
            if (upload is null)
            {
                return Results.Problem(
                    $"'{file.FileName}' is not a DICOM file. Nothing was imported.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            uploads.Add(upload);
        }

        var existingUids = await db.Instances
            .Where(i => i.StudyId == studyId)
            .Select(i => i.SopInstanceUid)
            .ToListAsync();

        var newUploads = uploads
            .Where(u => !existingUids.Contains(u.SopInstanceUid))
            .DistinctBy(u => u.SopInstanceUid)
            .ToList();

        foreach (var upload in newUploads)
        {
            var storagePath = $"{studyId}/{Guid.CreateVersion7():N}.dcm";
            await storage.SaveAsync(upload.File, storagePath);

            db.Instances.Add(new Instance
            {
                Id = Guid.CreateVersion7(),
                StudyId = studyId,
                SeriesInstanceUid = upload.SeriesInstanceUid,
                SopInstanceUid = upload.SopInstanceUid,
                InstanceNumber = upload.InstanceNumber,
                StoragePath = storagePath,
                SizeBytes = upload.File.Length,
            });
        }

        await db.SaveChangesAsync();

        if (!await studies.MarkImagesImportedAsync(studyId))
        {
            return Results.Problem(
                "The images are stored, but the study could not be marked as imported. Upload the same files again to retry.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new ImportResult(newUploads.Count, uploads.Count - newUploads.Count));
    }

    private static async Task<IResult> ListInstances(Guid studyId, ImagingDbContext db)
    {
        var instances = await db.Instances
            .Where(i => i.StudyId == studyId)
            .OrderBy(i => i.SeriesInstanceUid)
            .ThenBy(i => i.InstanceNumber)
            .Select(i => new InstanceSummary(i.Id, i.SeriesInstanceUid))
            .ToListAsync();

        return Results.Ok(instances);
    }

    private static async Task<IResult> GetFile(Guid id, ImagingDbContext db, ImageStorage storage)
    {
        var instance = await db.Instances.FindAsync(id);
        if (instance is null)
        {
            return Results.NotFound();
        }

        return Results.File(storage.GetFullPath(instance.StoragePath), "application/dicom");
    }

    private static async Task<DicomUpload?> ReadDicomAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var dicom = await DicomFile.OpenAsync(stream, FileReadOption.SkipLargeTags);

            var seriesUid = dicom.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, "");
            var sopUid = dicom.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, "");
            var instanceNumber = dicom.Dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, 0);

            if (seriesUid == "" || sopUid == "")
            {
                return null;
            }

            return new DicomUpload(file, seriesUid, sopUid, instanceNumber);
        }
        catch (DicomException)
        {
            return null;
        }
    }
}
