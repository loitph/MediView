namespace MediView.Imaging.Application.Instances;

public interface IBlobStore
{
    public Task<long> SaveAsync(string key, Stream content, CancellationToken cancellationToken);

    public Stream OpenRead(string key);

    public void Delete(string key);
}
