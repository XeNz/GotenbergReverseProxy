using GotenbergReverseProxy.Features;
using GotenbergReverseProxy.FormFile;

namespace GotenbergReverseProxy.Handlers;

internal interface IConvertAndMergeHandler
{
    public Task HandleAsync(
        List<FormFileStreamHolder> formFileStreamHolders,
        GeneratePdfAndMergeFeatures generatePdfAndMergeFeatures,
        CancellationToken cancellationToken);
}