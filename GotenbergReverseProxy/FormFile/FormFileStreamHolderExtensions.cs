using GotenbergReverseProxy.Features;

namespace GotenbergReverseProxy.FormFile;

internal static class FormFileStreamHolderExtensions
{
    internal static async Task<List<FormFileStreamHolder>> ExtractFormFileStreams(this IFormCollection form)
    {
        var formFileStreamHolders = new List<FormFileStreamHolder>(form.Files.Count);
        
        await Task.WhenAll(form.Files
            .Select(async formFile =>
            {
                var formFileStreamHolder = new FormFileStreamHolder(new MemoryStream());
                await formFile.CopyToAsync(formFileStreamHolder.Stream);
                formFileStreamHolder.Stream.Seek(0, SeekOrigin.Begin);
                formFileStreamHolders.Add(formFileStreamHolder);
            })
            .ToList());

        return formFileStreamHolders;
    }
}