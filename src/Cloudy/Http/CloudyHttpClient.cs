using System.Net;
using Cloudy.Models.Checker;
using Cloudy.Models.Proxies;

namespace Cloudy.Http;

public class CloudyHttpClient : HttpClient
{
    public Proxy? Proxy { get; }
    
    protected CloudyHttpClient(Proxy proxy, HttpClientHandler handler) : base(handler)
    {
        Proxy = proxy;
        handler.Proxy = new WebProxy(proxy.ToString());
        handler.Credentials = proxy.Credentials;
        handler.UseProxy = true;
    }
    
    protected CloudyHttpClient(HttpMessageHandler handler) : base(handler)
    {
    }
    
    public static CloudyHttpClient BuildFromProxy(Proxy proxy, ProxySettings proxySettings, CheckerSettings checkerSettings)
    {
        var handler = GetHttpClientHandler(checkerSettings);
        var httpClient = new CloudyHttpClient(proxy, handler)
        {
            Timeout = proxySettings.Timeout
        };

        httpClient.DefaultRequestHeaders.ConnectionClose = proxySettings.Rotating;

        return httpClient;
    }

    public static CloudyHttpClient Build(CheckerSettings checkerSettings)
    {
        return new(GetHttpClientHandler(checkerSettings));
    }

    private static HttpClientHandler GetHttpClientHandler(CheckerSettings checkerSettings)
    {
        var handler = new HttpClientHandler()
        {
            UseCookies = checkerSettings.UseCookies,
            AllowAutoRedirect = checkerSettings.AllowAutoRedirect,
            MaxAutomaticRedirections = checkerSettings.MaxAutoRedirections
        };

        if (checkerSettings.SslProtocol.HasValue)
        {
            handler.SslProtocols = checkerSettings.SslProtocol.Value;
        }

        return handler;
    }
}