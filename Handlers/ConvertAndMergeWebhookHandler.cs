using GotenbergReverseProxy.Features;
using GotenbergReverseProxy.FormFile;
using GotenbergReverseProxy.Settings;
using Microsoft.Extensions.Options;

namespace GotenbergReverseProxy.Handlers;

internal class ConvertAndMergeWebhookHandler : IConvertAndMergeHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<ForwardSettings> _forwardSettingsOptionsMonitor;
    private readonly ILogger<ConvertAndMergeWebhookHandler> _logger;

    public ConvertAndMergeWebhookHandler(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<ForwardSettings> forwardSettingsOptionsMonitor,
        ILogger<ConvertAndMergeWebhookHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _forwardSettingsOptionsMonitor = forwardSettingsOptionsMonitor;
        _logger = logger;
    }

    public async Task HandleAsync(
        List<FormFileStreamHolder> formFileStreamHolders,
        GeneratePdfAndMergeFeatures generatePdfAndMergeFeatures,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var fileStream = await ConvertUrlToPdf(httpClient, generatePdfAndMergeFeatures, cancellationToken);

        var completeFile = await MergeConvertedFileWithAdditionalFiles(
            formFileStreamHolders,
            fileStream,
            httpClient,
            cancellationToken
        );

        await SendCallbackWithCompleteFile(
            generatePdfAndMergeFeatures,
            completeFile,
            httpClient,
            cancellationToken
        );
    }

    /// <summary>
    /// Executes HTTP call that calls a service that merges the initially converted file with other included files 
    /// </summary>
    /// <param name="formFileStreamHolders"></param>
    /// <param name="fileStream"></param>
    /// <param name="httpClient"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<HttpResponseMessage> MergeConvertedFileWithAdditionalFiles(
        List<FormFileStreamHolder> formFileStreamHolders,
        Stream fileStream,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage httpResponseMessage;

        try
        {
            var multipartFormDataContent = new MultipartFormDataContent
            {
                { new StreamContent(fileStream), "files", "1.pdf" }
            };

            // add extra files 
            for (var index = 0; index < formFileStreamHolders.Count; index++)
            {
                var formFile = formFileStreamHolders[index];
                multipartFormDataContent.Add(new StreamContent(formFile.Stream), "files", $"{index + 2}.pdf");
            }

            httpResponseMessage = await httpClient.PostAsync(GetMergeUrl(), multipartFormDataContent, cancellationToken);
            httpResponseMessage.EnsureSuccessStatusCode(); // TODO throw custom exception
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Something went wrong trying to merge converted file with additional files");
            throw;
        }
        finally
        {
            foreach (var formFileStreamHolder in formFileStreamHolders)
                await formFileStreamHolder.DisposeAsync();

            await fileStream.DisposeAsync();
        }

        return httpResponseMessage;
    }


    /// <summary>
    /// Executes HTTP call that calls a service that converts the url that is passed in through a header to our main file to which other files need to be appended to.  
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="generatePdfAndMergeFeatures"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Stream> ConvertUrlToPdf(
        HttpClient httpClient,
        GeneratePdfAndMergeFeatures generatePdfAndMergeFeatures,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage httpResponseMessage;
        try
        {
            var multipartFormDataContent = new MultipartFormDataContent
            {
                { new StringContent(generatePdfAndMergeFeatures.PdfUrlToGenerate!), "url" }
            };

            httpResponseMessage = await httpClient.PostAsync(GetConvertUrl(), multipartFormDataContent, cancellationToken);
            httpResponseMessage.EnsureSuccessStatusCode(); // TODO throw custom exception
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Something went wrong trying to convert url to pdf");
            throw;
        }

        return await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a Callback HTTP call containing the completely merged file to the origin.
    /// </summary>
    /// <param name="generatePdfAndMergeFeatures"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="completeFile"></param>
    /// <param name="httpClient"></param>
    private static async Task SendCallbackWithCompleteFile(
        GeneratePdfAndMergeFeatures generatePdfAndMergeFeatures,
        HttpResponseMessage completeFile,
        HttpMessageInvoker httpClient,
        CancellationToken cancellationToken)
    {
        var multipartFormDataContent = new MultipartFormDataContent
        {
            {
                new StreamContent(await completeFile.Content.ReadAsStreamAsync(cancellationToken)),
                "files",
                generatePdfAndMergeFeatures.OutputFilename!
            }
        };

        var httpRequestMessage = new HttpRequestMessage
        {
            Content = multipartFormDataContent,
            Method = generatePdfAndMergeFeatures.WebhookMethod!,
            RequestUri = new Uri(generatePdfAndMergeFeatures.WebhookUrl!)
        };

        var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
        httpResponseMessage.EnsureSuccessStatusCode(); // TODO throw custom exception
    }

    private string GetConvertUrl() => $"{_forwardSettingsOptionsMonitor.CurrentValue.GotenbergInstanceUrl}/forms/chromium/convert/url";
    private string GetMergeUrl() => $"{_forwardSettingsOptionsMonitor.CurrentValue.GotenbergInstanceUrl}/forms/pdfengines/merge";
}