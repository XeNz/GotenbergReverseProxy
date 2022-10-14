namespace GotenbergReverseProxy.Features;

public class GeneratePdfAndMergeFeatures
{
    public string? OutputFilename { get; set; }
    public string? PdfUrlToGenerate { get; set; }
    public string? WebhookUrl { get; set; }
    public HttpMethod? WebhookMethod { get; set; }
    public Guid GenerationId { get; set; }
}