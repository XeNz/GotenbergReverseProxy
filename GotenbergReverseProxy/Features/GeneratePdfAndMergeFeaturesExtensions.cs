using GotenbergReverseProxy.Constants;

namespace GotenbergReverseProxy.Features;

public static class GeneratePdfAndMergeFeaturesExtensions
{
    public static GeneratePdfAndMergeFeatures GetGeneratePdfAndMergeFeaturesFromRequest(this HttpRequest request, IFormCollection formCollection)
    {
        var headers = request.Headers;
        var features = new GeneratePdfAndMergeFeatures();

        if (headers.ContainsKey(GotenbergHeaders.Trace))
        {
            features.GenerationId = new Guid(headers[GotenbergHeaders.Trace][0] ?? throw new InvalidOperationException());
        }

        if (headers.ContainsKey(GotenbergHeaders.OutputFilename))
        {
            features.OutputFilename = headers[GotenbergHeaders.OutputFilename][0];
        }

        if (headers.ContainsKey(GotenbergHeaders.WebhookMethod) && headers.ContainsKey(GotenbergHeaders.WebhookUrl))
        {
            features.WebhookMethod = new HttpMethod(headers[GotenbergHeaders.WebhookMethod][0] ?? throw new InvalidOperationException());
            features.WebhookUrl = headers[GotenbergHeaders.WebhookUrl][0] ?? throw new InvalidOperationException();
        }
        
        features.PdfUrlToGenerate = formCollection["url"][0];

        return features;
    }
}