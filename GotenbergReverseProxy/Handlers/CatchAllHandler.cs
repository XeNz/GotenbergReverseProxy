using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace GotenbergReverseProxy.Handlers;

internal static class CatchAllDelegate
{
    public static async Task ForwardRequest(
        string forwardUrl,
        IHttpForwarder httpForwarder,
        HttpContext context,
        HttpMessageInvoker httpMessageInvoker,
        ForwarderRequestConfig forwarderRequestConfig)
    {
        var error = await httpForwarder.SendAsync(context,
            forwardUrl,
            httpMessageInvoker,
            forwarderRequestConfig,
            (_, proxyRequest) =>
            {
                var queryContext = new QueryTransformContext(context.Request);
                proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(forwardUrl,context.Request.Path, queryContext.QueryString);
                proxyRequest.Headers.Host = null;

                return default;
            });

        // Check if the proxy operation was successful
        if (error != ForwarderError.None)
        {
            var errorFeature = context.Features.Get<IForwarderErrorFeature>();
            var exception = errorFeature?.Exception;
        }
    }
}