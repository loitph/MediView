namespace MediView.Imaging.Api;

public class ImageStorage(string rootFolder)
{
    public string GetFullPath(string storagePath) => Path.Combine(rootFolder, storagePath);

    public async Task SaveAsync(IFormFile file, string storagePath)
    {
        var fullPath = GetFullPath(storagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var target = File.Create(fullPath);
        await file.CopyToAsync(target);
    }
}
