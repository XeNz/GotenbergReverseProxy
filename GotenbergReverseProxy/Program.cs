using System.Net.Http.Headers;
using GotenbergReverseProxy.Features;
using GotenbergReverseProxy.FormFile;
using GotenbergReverseProxy.Forwarding;
using GotenbergReverseProxy.Handlers;
using GotenbergReverseProxy.Settings;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpForwarder();
builder.Services.AddHttpClient();
builder.Services.RegisterConvertAndMergeHandler();
builder.Services.Configure<ForwardSettings>(builder.Configuration.GetSection(nameof(ForwardSettings)));
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseHttpsRedirection();
app.MapHealthChecks("/health");

app.UseRouting();

var forwardingProxy = new ForwardingProxy(app.Services.GetRequiredService<IHttpForwarder>());
app.UseEndpoints(endpoints =>
{
    endpoints.Map("/{**catch-all}",
        async (HttpContext httpContext,
                IOptionsMonitor<ForwardSettings> forwardSettings) =>
            await CatchAllHandler.ForwardRequest(
                forwardSettings.CurrentValue.GotenbergInstanceUrl,
                forwardingProxy.HttpForwarder,
                httpContext,
                forwardingProxy.HttpMessageInvoker,
                forwardingProxy.ForwarderRequestConfig)
    );


    endpoints.MapPost("/forms/chromium/convertAndMerge/url",
        async (HttpContext context,
            IConvertAndMergeHandler convertAndMergeHandler,
            CancellationToken cancellationToken) =>
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);

            var pdfAndMergeFeatures = context.Request.GetGeneratePdfAndMergeFeaturesFromRequest(form);
            var formFileStreamHolders = await form.ExtractFormFileStreams();

            _ = Task.Run(async () => await convertAndMergeHandler.HandleAsync(
                    formFileStreamHolders,
                    pdfAndMergeFeatures,
                    cancellationToken
                ),
                cancellationToken);
            return Results.Accepted();
        });

#if (DEBUG)
    endpoints.MapPost("/test",
        async (HttpContext context, ILogger<Program> logger) =>
        {
            logger.LogDebug("Got new test callback request");
            var contentDisposition = ContentDispositionHeaderValue.Parse(context.Request.Headers.ContentDisposition);
            
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fileNameWithPath = Path.Combine(path, contentDisposition.FileName ?? throw new InvalidOperationException());

            using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
            {
                await context.Request.Body.CopyToAsync(stream);
            }

            return Results.Accepted();
        });
#endif
});

app.Run();