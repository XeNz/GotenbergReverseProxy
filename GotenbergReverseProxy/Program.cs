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
            await CatchAllDelegate.ForwardRequest(
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
            var pdfAndMergeFeatures = context.Request.GetGeneratePdfAndMergeFeaturesFromRequest();
            var formFileStreamHolders = await context.ConvertIFormFileDetails(context.Request.Form.Files.Count, pdfAndMergeFeatures);

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
            logger.LogDebug("Got new test callback request, got {} form files", context.Request.Form.Files.Count);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            foreach (var formFile in context.Request.Form.Files)
            {
                var fileNameWithPath = Path.Combine(path, formFile.FileName);

                using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                {
                    await formFile.CopyToAsync(stream);
                }
            }

            return Results.Accepted();
        });
#endif
});

app.Run();