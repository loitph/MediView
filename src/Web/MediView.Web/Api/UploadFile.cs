namespace MediView.Web.Api;

public sealed record UploadFile(string FileName, long Size, Func<Stream> OpenRead);
