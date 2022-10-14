using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace GotenbergReverseProxy.Forwarding;

public class ForwardingProxy
{
    public ForwardingProxy(IHttpForwarder httpForwarder)
    {
        HttpMessageInvoker = new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.Brotli,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
        });

        HttpForwarder = httpForwarder;
        ForwarderRequestConfig = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(100) };
    }

    public HttpMessageInvoker HttpMessageInvoker { get; }
    public IHttpForwarder HttpForwarder { get; }
    public ForwarderRequestConfig ForwarderRequestConfig { get; }
}