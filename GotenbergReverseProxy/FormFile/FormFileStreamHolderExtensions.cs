using GotenbergReverseProxy.Features;

namespace GotenbergReverseProxy.FormFile;

internal static class FormFileStreamHolderExtensions
{
    internal static async Task<List<FormFileStreamHolder>> ConvertIFormFileDetails(
        this HttpContext httpContext,
        int count,
        GeneratePdfAndMergeFeatures features)
    {
        features.PdfUrlToGenerate = httpContext.Request.Form["url"][0];

        var formFileStreamHolders = new List<FormFileStreamHolder>(count);

        await Task.WhenAll(httpContext.Request.Form.Files
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