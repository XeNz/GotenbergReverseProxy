namespace GotenbergReverseProxy.FormFile;

public sealed class FormFileStreamHolder : IAsyncDisposable, IDisposable
{
    public FormFileStreamHolder(MemoryStream stream) => Stream = stream;

    public MemoryStream Stream { get; }

    public void Dispose() => Stream.Dispose();
    public async ValueTask DisposeAsync() => await Stream.DisposeAsync();
}